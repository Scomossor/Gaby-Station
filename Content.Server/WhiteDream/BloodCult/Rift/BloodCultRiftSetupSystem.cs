// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Goobstation.Maths.FixedPoint;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Pinpointer;
using Content.Server.Station.Systems;
using Content.Server.WhiteDream.BloodCult.Gamerule;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Maps;
using Content.Shared.Mind.Components;
using Content.Shared.Physics;
using Content.Shared.Pinpointer;
using Content.Shared.SubFloor;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.WhiteDream.BloodCult.Rift;

/// <summary>
///     Finds somewhere on the station for the blood rift to bleed through, clears a 3x3 pad for it
///     and spawns the rift plus its four offering runes.
/// </summary>
public sealed partial class BloodCultRiftSetupSystem : EntitySystem
{
    // Anything outside these ranges would kill the cultists standing on the runes, so we look elsewhere.
    private const float MinPressureKpa = 50f;
    private const float MaxPressureKpa = 300f;
    private const float MinTemperatureK = 150f;
    private const float MaxTemperatureK = 300f;

    private const int BeaconAttempts = 10;
    private const float BeaconSpread = 10f;

    private const string RiftFloor = "FloorHullReinforced";
    private static readonly EntProtoId SummoningRune = "CultRuneFinalSummoning";

    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ITileDefinitionManager _tileDef = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private MapSystem _map = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private StationSystem _station = default!;

    /// <summary>
    ///     Tries every strategy in turn and returns the rift, or null if the station refused all of them.
    /// </summary>
    public EntityUid? TrySetupRitualSite(BloodCultRuleComponent rule)
    {
        // 1. A random spot near a random department beacon. This is the nice case.
        var beacons = GetBeacons();
        _random.Shuffle(beacons);

        foreach (var beacon in beacons)
        {
            for (var attempt = 0; attempt < BeaconAttempts; attempt++)
            {
                if (!TrySpawnNearBeacon(rule, beacon, out var rift))
                    continue;

                rule.RiftLocation = GetLocationName(rift);
                return rift;
            }
        }

        // 2. Somewhere near a cultist.
        var cultists = GetCultists();
        _random.Shuffle(cultists);

        foreach (var cultist in cultists)
        {
            if (!TryFindValid3X3(Transform(cultist).Coordinates, out var center, out var gridUid, out var grid))
                continue;

            ReplaceFlooring(gridUid, grid, center);
            var rift = SpawnRiftAndRunes(rule, center);
            rule.RiftLocation = GetLocationName(rift);
            return rift;
        }

        // 3. Force it open on top of a cultist, tearing out whatever is in the way.
        foreach (var cultist in GetCultists())
        {
            var coords = Transform(cultist).Coordinates;
            if (!TryResolveGrid(coords, out var gridUid, out var grid))
                continue;

            ClearBlockingEntities(gridUid, grid, coords);
            ReplaceFlooring(gridUid, grid, coords);
            var rift = SpawnRiftAndRunes(rule, coords);
            rule.RiftLocation = GetLocationName(rift);
            return rift;
        }

        // 4. Force it open on a beacon.
        foreach (var beacon in beacons)
        {
            var coords = Transform(beacon).Coordinates;
            if (!TryResolveGrid(coords, out var gridUid, out var grid))
                continue;

            ClearBlockingEntities(gridUid, grid, coords);
            ReplaceFlooring(gridUid, grid, coords);
            var rift = SpawnRiftAndRunes(rule, coords);
            rule.RiftLocation = GetLocationName(rift);
            return rift;
        }

        return null;
    }

    private bool TrySpawnNearBeacon(BloodCultRuleComponent rule, EntityUid beacon, out EntityUid? rift)
    {
        rift = null;

        var beaconXform = Transform(beacon);
        var anchor = beaconXform.GridUid ?? beaconXform.MapUid;
        if (anchor is not { Valid: true } anchorUid)
            return false;

        var offset = new Vector2(_random.NextFloat(-BeaconSpread, BeaconSpread),
            _random.NextFloat(-BeaconSpread, BeaconSpread));
        var target = new EntityCoordinates(anchorUid, beaconXform.Coordinates.Position + offset);

        if (!TryFindValid3X3(target, out var center, out var gridUid, out var grid))
            return false;

        ReplaceFlooring(gridUid, grid, center);
        rift = SpawnRiftAndRunes(rule, center);
        return true;
    }

    private List<EntityUid> GetBeacons()
    {
        var beacons = new List<EntityUid>();
        var query = EntityQueryEnumerator<NavMapBeaconComponent>();
        while (query.MoveNext(out var uid, out var beacon))
        {
            if (!beacon.Enabled)
                continue;

            // WhiteDream - station beacons only, otherwise the rift opens on an outpost, a piece of
            // debris or the escape shuttle instead of on the station.
            if (_station.GetOwningStation(uid) is null)
                continue;

            beacons.Add(uid);
        }

        return beacons;
    }

    private List<EntityUid> GetCultists()
    {
        var cultists = new List<EntityUid>();
        var query = EntityQueryEnumerator<BloodCultistComponent>();
        while (query.MoveNext(out var uid, out _))
            cultists.Add(uid);

        return cultists;
    }

    private string GetLocationName(EntityUid? rift)
    {
        if (rift is not { Valid: true } riftUid)
            return "Unknown";
        
        return FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(riftUid));
    }

    private bool TryResolveGrid(EntityCoordinates coords, out EntityUid gridUid, out MapGridComponent grid)
    {
        gridUid = EntityUid.Invalid;
        grid = default!;

        if (TryComp<MapGridComponent>(coords.EntityId, out var direct))
        {
            gridUid = coords.EntityId;
            grid = direct;
            return true;
        }

        if (_transform.GetGrid(coords) is not { } resolved || !TryComp<MapGridComponent>(resolved, out var comp))
            return false;

        gridUid = resolved;
        grid = comp;
        return true;
    }

    private bool TryFindValid3X3(
        EntityCoordinates around,
        out EntityCoordinates center,
        out EntityUid gridUid,
        out MapGridComponent grid
    )
    {
        center = EntityCoordinates.Invalid;

        if (!TryResolveGrid(around, out gridUid, out grid))
            return false;

        var origin = _map.TileIndicesFor(gridUid, grid, around);

        for (var x = -5; x <= 5; x++)
        {
            for (var y = -5; y <= 5; y++)
            {
                var candidate = new Vector2i(origin.X + x, origin.Y + y);
                if (!IsValid3X3(gridUid, grid, candidate))
                    continue;

                center = _map.GridTileToLocal(gridUid, grid, candidate);
                return true;
            }
        }

        return false;
    }

    private bool IsValid3X3(EntityUid gridUid, MapGridComponent grid, Vector2i center)
    {
        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                if (!IsTileValid(gridUid, grid, new Vector2i(center.X + x, center.Y + y)))
                    return false;
            }
        }

        return true;
    }

    private bool IsTileValid(EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        if (_map.GetTileRef(gridUid, grid, tile).Tile.IsEmpty)
            return false;

        var mapUid = Transform(gridUid).MapUid;
        var mixture = _atmosphere.GetTileMixture(gridUid, mapUid, tile);
        if (mixture is null)
            return false;

        if (mixture.Pressure < MinPressureKpa || mixture.Pressure > MaxPressureKpa)
            return false;

        if (mixture.Temperature < MinTemperatureK || mixture.Temperature > MaxTemperatureK)
            return false;

        foreach (var anchored in _map.GetAnchoredEntities(gridUid, grid, tile))
        {
            // Cables and pipes are fine, they're under the floor.
            if (HasComp<SubFloorHideComponent>(anchored))
                continue;

            if (TryComp<PhysicsComponent>(anchored, out var physics) && IsBlocking(physics))
                return false;
        }

        return true;
    }

    private static bool IsBlocking(PhysicsComponent physics)
    {
        const CollisionGroup blocking = CollisionGroup.Impassable
                                        | CollisionGroup.WallLayer
                                        | CollisionGroup.GlassLayer
                                        | CollisionGroup.FullTileLayer
                                        | CollisionGroup.AirlockLayer
                                        | CollisionGroup.GlassAirlockLayer;

        return (physics.CollisionLayer & (int) blocking) != 0;
    }

    /// <summary>
    ///     Last resort: rip out every wall and airlock in the 3x3. Never touches players.
    /// </summary>
    private void ClearBlockingEntities(EntityUid gridUid, MapGridComponent grid, EntityCoordinates center)
    {
        var origin = _map.TileIndicesFor(gridUid, grid, center);

        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                var tile = new Vector2i(origin.X + x, origin.Y + y);
                foreach (var anchored in _map.GetAnchoredEntities(gridUid, grid, tile).ToList())
                {
                    // Never delete anything that could be a player.
                    if (HasComp<MindContainerComponent>(anchored))
                        continue;

                    if (TryComp<PhysicsComponent>(anchored, out var physics) && IsBlocking(physics))
                        QueueDel(anchored);
                }
            }
        }
    }

    /// <summary>
    ///     The rift has to sit on a grid that can't be cut away, and it needs open neighbours for the runes.
    /// </summary>
    private void ReplaceFlooring(EntityUid gridUid, MapGridComponent grid, EntityCoordinates center)
    {
        var origin = _map.TileIndicesFor(gridUid, grid, center);
        var tileDef = (ContentTileDefinition) _tileDef[RiftFloor];
        var tile = new Tile(tileDef.TileId);

        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
                _map.SetTile(gridUid, grid, new Vector2i(origin.X + x, origin.Y + y), tile);
        }
    }

    private EntityUid SpawnRiftAndRunes(BloodCultRuleComponent rule, EntityCoordinates center)
    {
        var rift = Spawn(rule.RiftPrototype, center);
        var riftComp = EnsureComp<BloodCultRiftComponent>(rift);
        riftComp.SummoningRunes.Clear();

        // Four summoning runes around the rift, cardinal directions.
        foreach (var offset in new[]
                 {
                     new Vector2(-1, 0),
                     new Vector2(1, 0),
                     new Vector2(0, -1),
                     new Vector2(0, 1),
                 })
        {
            var rune = Spawn(SummoningRune, center.Offset(offset));
            EnsureComp<FinalSummoningRuneComponent>(rune).Rift = rift;
            riftComp.SummoningRunes.Add(rune);
        }

        // Prefill the pool so the rift starts bleeding immediately instead of after a few pulses.
        if (_solution.TryGetSolution(rift, BloodCultRiftComponent.SolutionName, out var solutionEnt, out var solution))
        {
            var deficit = solution.MaxVolume - solution.Volume;
            if (deficit > FixedPoint2.Zero)
                _solution.TryAddReagent(solutionEnt.Value, BloodCultRiftComponent.Reagent, deficit, out _);
        }

        return rift;
    }
}
