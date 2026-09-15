// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Content.Shared.WhiteDream.BloodCult.Runes;

namespace Content.Server.WhiteDream.BloodCult.CultBarrier;

public sealed partial class BloodCultBarrierSystem : EntitySystem
{
    [Dependency] private PopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BloodCultBarrierComponent, InteractUsingEvent>(OnInteract);
    }

    private void OnInteract(Entity<BloodCultBarrierComponent> ent, ref InteractUsingEvent args)
    {
        if (!HasComp<RuneDrawerComponent>(args.Used) || !HasComp<BloodCultistComponent>(args.User))
            return;

        // Dumont
        _popup.PopupEntity(Loc.GetString("cult-barrier-destroyed"), args.User, args.User);
        Del(args.Target);
    }
}
