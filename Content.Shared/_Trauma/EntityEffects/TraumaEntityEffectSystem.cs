// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// </summary>
/// <typeparam name="TComp">The component the target needs for the effect to do anything.</typeparam>
/// <typeparam name="TEffect">The effect handled.</typeparam>
public abstract class TraumaEntityEffectSystem<TComp, TEffect> : EntitySystem
    where TComp : IComponent
    where TEffect : EventEntityEffect<TEffect>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExecuteEntityEffectEvent<TEffect>>(OnExecute);
    }

    private void OnExecute(ref ExecuteEntityEffectEvent<TEffect> args)
    {
        var target = args.Args.TargetEntity;
        if (!TryComp<TComp>(target, out var comp))
            return;

        Effect((target, comp), args.Effect, args.Args);
    }

    protected abstract void Effect(Entity<TComp> ent, TEffect effect, EntityEffectBaseArgs args);
}
