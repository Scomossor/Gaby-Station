// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Construction;
using Content.Shared.Construction.Prototypes;
using Content.Shared.WhiteDream.BloodCult.Construction;
using Robust.Client.Placement;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.WhiteDream.BloodCult.Construction;

public sealed partial class BloodCultConstructionSystem : EntitySystem
{
    [Dependency] private ConstructionSystem _construction = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private IPlacementManager _placement = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultConstructionComponent, BloodCultConstructionSelectedMessage>(OnItemReceived);
    }

    private void OnItemReceived(Entity<BloodCultConstructionComponent> ent, ref BloodCultConstructionSelectedMessage args)
    {
        if (!_proto.TryIndex(args.SelectedItem, out ConstructionPrototype? prototype) ||
            !_gameTiming.IsFirstTimePredicted)
            return;

        if (prototype.Type == ConstructionType.Item)
        {
            _construction.TryStartItemConstruction(prototype.ID);
            return;
        }

        var hijack = new ConstructionPlacementHijack(_construction, prototype); // Dumont
        _placement.BeginPlacing(new PlacementInformation
            {
                IsTile = false,
                PlacementOption = prototype.PlacementMode,
            },
            hijack);
    }
}
