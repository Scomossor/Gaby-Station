// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.UserInterface;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;

namespace Content.Shared.WhiteDream.BloodCult.Construction;

public sealed class SharedBloodCultConstructionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultConstructionComponent, ActivatableUIOpenAttemptEvent>(OnOpenAttempt);
    }

    private void OnOpenAttempt(Entity<BloodCultConstructionComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (!HasComp<BloodCultistComponent>(args.User))
            args.Cancel();
    }
}
