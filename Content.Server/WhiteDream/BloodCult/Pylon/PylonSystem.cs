// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Server.Popups;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Content.Shared.WhiteDream.BloodCult;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Content.Shared.WhiteDream.BloodCult.Components;
using Content.Shared._Shitmed.Damage;
using Content.Shared._Shitmed.Targeting;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.WhiteDream.BloodCult.Pylon;

public sealed partial class PylonSystem : EntitySystem
{
    [Dependency] private ITileDefinitionManager _tileDefinition = default!;
    [Dependency] private IRobustRandom _random = default!;

    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private MapSystem _map = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private TileSystem _tile = default!;
    [Dependency] private TurfSystem _turfs = default!;
    [Dependency] private PointLightSystem _pointLight = default!;
    [Dependency] private PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PylonComponent, InteractHandEvent>(OnInteract);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var pylonQuery = EntityQueryEnumerator<PylonComponent>();
        while (pylonQuery.MoveNext(out var uid, out var pylon))
        {
            if (!pylon.IsActive)
                continue;

            pylon.CorruptionAccumulator += frameTime;
            pylon.HealingAccumulator += frameTime;

            if (pylon.CorruptionAccumulator >= pylon.CorruptionCooldown)
            {
                pylon.CorruptionAccumulator = 0;
                CorruptTilesInRange((uid, pylon));
            }

            if (pylon.HealingAccumulator >= pylon.HealingCooldown)
            {
                pylon.HealingAccumulator = 0;
                HealInRange((uid, pylon));
            }
        }
    }

    private void OnInteract(Entity<PylonComponent> pylon, ref InteractHandEvent args)
    {
        if (!HasComp<BloodCultistComponent>(args.User))
        {
            _audio.PlayEntity(pylon.Comp.BurnHandSound, Filter.Pvs(pylon), pylon, true);
            _popup.PopupEntity(Loc.GetString("powered-light-component-burn-hand"), pylon, args.User);
            _damageable.TryChangeDamage(args.User, pylon.Comp.DamageOnInteract, true);
            return;
        }

        ToggleActive(pylon);
        var toggleMsg = Loc.GetString(pylon.Comp.IsActive ? "pylon-toggle-on" : "pylon-toggle-off");
        _popup.PopupEntity(toggleMsg, pylon);
    }

    private void ToggleActive(Entity<PylonComponent> pylon)
    {
        var state = !pylon.Comp.IsActive;
        pylon.Comp.IsActive = state;
        _appearance.SetData(pylon, PylonVisuals.Activated, state);
        _pointLight.SetEnabled(pylon, state);
    }

    private void CorruptTilesInRange(Entity<PylonComponent> pylon)
    {
        var pylonTrans = Transform(pylon);
        if (pylonTrans.GridUid is not { } gridUid || !TryComp(pylonTrans.GridUid, out MapGridComponent? mapGrid))
            return;

        var radius = pylon.Comp.CorruptionRadius;
        var tilesRefs = _map.GetLocalTilesIntersecting(gridUid,
                mapGrid,
                new Box2(pylonTrans.Coordinates.Position + new Vector2(-radius, -radius),
                    pylonTrans.Coordinates.Position + new Vector2(radius, radius)))
            .ToList();

        _random.Shuffle(tilesRefs);

        var cultTileDefinition = (ContentTileDefinition) _tileDefinition[pylon.Comp.CultTile];
        foreach (var tile in tilesRefs)
        {
            if (tile.Tile.TypeId == cultTileDefinition.TileId)
                continue;

            var tilePos = _turfs.GetTileCenter(tile);
            _audio.PlayPvs(pylon.Comp.CorruptTileSound, tilePos, AudioParams.Default.WithVolume(-5));
            _tile.ReplaceTile(tile, cultTileDefinition);
            Spawn(pylon.Comp.TileCorruptEffect, tilePos);
            return;
        }
    }

    private void HealInRange(Entity<PylonComponent> pylon)
    {
        var pylonPosition = Transform(pylon).Coordinates;
        var targets =
            _lookup.GetEntitiesInRange<BloodCultistComponent>(pylonPosition, pylon.Comp.HealingAuraRange);

        foreach (var target in targets)
        {
            if (_mobState.IsDead(target))
                continue;

            // Dumont
            _damageable.TryChangeDamage(target.Owner,
                pylon.Comp.Healing,
                true,
                false,
                targetPart: TargetBodyPart.All,
                splitDamage: SplitDamageBehavior.SplitEnsureAllOrganic,
                canMiss: false);

            // Dumont
            _bloodstream.TryModifyBloodLevel(target.Owner, pylon.Comp.BloodRegenerationAmount);
        }
    }
}
