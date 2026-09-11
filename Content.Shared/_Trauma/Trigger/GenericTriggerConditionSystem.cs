// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Trigger;
using Content.Trauma.Shared.EntityEffects;

namespace Content.Trauma.Shared.Trigger;

public sealed partial class GenericTriggerConditionSystem : EntitySystem
{
    [Dependency] private TraumaEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GenericTriggerConditionComponent, AttemptTriggerEvent>(OnAttemptTrigger);
    }

    private void OnAttemptTrigger(Entity<GenericTriggerConditionComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Key is not { } key || !ent.Comp.Keys.Contains(key))
            return;

        if ((ent.Comp.CheckUser ? args.User : ent.Owner) is not { } target)
        {
            args.Cancelled = true;
            return;
        }

        args.Cancelled |= !_effects.TryCondition(target, ent.Comp.Condition);
    }
}
