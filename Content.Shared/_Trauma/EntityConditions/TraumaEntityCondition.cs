// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityConditions;

public abstract partial class TraumaEntityCondition<T> : EventEntityEffectCondition<T>
    where T : TraumaEntityCondition<T>
{
    /// <summary>
    /// If true, invert the result. So false returns true and true returns false!
    /// </summary>
    [DataField]
    public bool Inverted;

    public override bool Condition(EntityEffectBaseArgs args)
        => Inverted != base.Condition(args);
}

/// <summary>
/// Equivalente do <c>EntityConditionSystem&lt;T, TCon&gt;</c> do Trauma: a condicao so e
/// </summary>
public abstract class TraumaEntityConditionSystem<TComp, TCond> : EntitySystem
    where TComp : IComponent
    where TCond : TraumaEntityCondition<TCond>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CheckEntityEffectConditionEvent<TCond>>(OnCheck);
    }

    private void OnCheck(ref CheckEntityEffectConditionEvent<TCond> args)
    {
        var target = args.Args.TargetEntity;
        if (!TryComp<TComp>(target, out var comp))
            return;

        args.Result = Condition((target, comp), args.Condition);
    }

    protected abstract bool Condition(Entity<TComp> ent, TCond condition);
}
