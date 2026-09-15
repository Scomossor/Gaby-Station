// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.PhaseShift;
using Content.Shared.WhiteDream.BloodCult.Spells;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Server.WhiteDream.BloodCult;

public sealed partial class ConstructActionsSystem : EntitySystem
{
    [Dependency] private ITileDefinitionManager _tileDef = default!;

    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private MapSystem _mapSystem = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private IGameTiming _timing = default!;

    private const string CultTileSpawnEffect = "CultTileSpawnEffect";

    public override void Initialize()
    {
        SubscribeLocalEvent<CultPlaceTileEntityEvent>(OnCultPlaceTileEntityEvent);
        SubscribeLocalEvent<PhaseShiftEvent>(OnPhaseShift);
    }

    private void OnCultPlaceTileEntityEvent(CultPlaceTileEntityEvent args)
    {
        if (args.Handled)
            return;

        if (args.EntityProto is { } entProtoId)
            Spawn(entProtoId, args.Target);

        if (args.TileId is { } tileId)
        {
            if (_transform.GetGrid(args.Target) is not { } grid || !TryComp(grid, out MapGridComponent? mapGrid))
                return;

            var tileDef = _tileDef[tileId];
            var tile = new Tile(tileDef.TileId);

            _mapSystem.SetTile(grid, mapGrid, args.Target, tile);
            Spawn(CultTileSpawnEffect, args.Target);
        }

        if (args.Audio is { } audio)
            _audio.PlayPvs(audio, args.Target);

        args.Handled = true;
    }

    private void OnPhaseShift(PhaseShiftEvent args)
    {
        if (args.Handled)
            return;

        EnsureComp<PhaseShiftedComponent>(args.Performer);
        EnsureComp<BloodCultPhasedComponent>(args.Performer).EndTime = _timing.CurTime + args.Duration;
        // </WhiteDream>

        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BloodCultPhasedComponent>();
        while (query.MoveNext(out var uid, out var phased))
        {
            if (_timing.CurTime < phased.EndTime)
                continue;

            RemComp<PhaseShiftedComponent>(uid);
            RemCompDeferred<BloodCultPhasedComponent>(uid);
        }
    }
}

/// <summary>
///     WhiteDream - tracks how long a construct stays phased out.
/// </summary>
[RegisterComponent]
public sealed partial class BloodCultPhasedComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan EndTime;
}
