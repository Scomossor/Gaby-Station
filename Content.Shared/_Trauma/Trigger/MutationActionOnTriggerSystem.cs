// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Trigger;
using Content.Trauma.Shared.Genetics.Mutations;

namespace Content.Trauma.Shared.Trigger.Effects;

public sealed partial class MutationActionOnTriggerSystem : EntitySystem
{
    [Dependency] private ActionMutationSystem _actionMutation = default!;
    [Dependency] private MutationSystem _mutation = default!;
    [Dependency] private SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MutationActionOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<MutationActionOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        if ((ent.Comp.TargetUser ? args.User : ent.Owner) == null)
            return;

        if (_mutation.GetMutationTarget(ent.Owner) is not {} user || _actionMutation.GetAction(ent.Owner) is not {} action)
            return;

        _actions.PerformAction(user, action);
        args.Handled = true;
    }
}
