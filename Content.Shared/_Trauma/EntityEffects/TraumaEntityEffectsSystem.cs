// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class TraumaEntityEffectsSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedEntityEffectSystem _effects = default!;

    /// <summary>
    /// Applies an entity effect to a target if its probability roll and all of its conditions pass.
    /// </summary>
    /// <returns>True if the effect was applied.</returns>
    public bool TryApplyEffect(EntityUid target, EntityEffect effect)
    {
        var args = new EntityEffectBaseArgs(target, EntityManager);
        if (!effect.ShouldApply(args, _random))
            return false;

        _effects.Effect(effect, args);
        return true;
    }

    /// <summary>
    /// Applies a list of entity effects to a target, in order, each one checking its own probability and conditions.
    /// </summary>
    public void ApplyEffects(EntityUid target, EntityEffect[] effects)
    {
        foreach (var effect in effects)
        {
            TryApplyEffect(target, effect);
        }
    }

    /// <summary>
    /// Applies the effects of an <see cref="EntityEffectPrototype"/> if the prototype's own conditions pass.
    /// </summary>
    /// <returns>True if the prototype's conditions passed.</returns>
    public bool TryApplyEffect(EntityUid target, ProtoId<EntityEffectPrototype> id)
    {
        var proto = _proto.Index(id);
        if (!TryConditions(target, proto.Conditions))
            return false;

        ApplyEffects(target, proto.Effects);
        return true;
    }

    /// <summary>
    /// Checks that every condition passes. No conditions means it passes.
    /// </summary>
    public bool TryConditions(EntityUid target, EntityEffectCondition[]? conditions)
    {
        if (conditions == null)
            return true;

        foreach (var condition in conditions)
        {
            if (!TryCondition(target, condition))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks a single condition on a target.
    /// </summary>
    public bool TryCondition(EntityUid target, EntityEffectCondition condition)
        => condition.Condition(new EntityEffectBaseArgs(target, EntityManager));

    /// <summary>
    /// Checks the condition of an <see cref="EntityConditionPrototype"/> on a target.
    /// </summary>
    public bool TryCondition(EntityUid target, ProtoId<EntityConditionPrototype> id)
        => TryCondition(target, _proto.Index(id).Condition);
}
