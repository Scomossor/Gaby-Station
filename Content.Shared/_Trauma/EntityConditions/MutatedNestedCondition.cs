// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.EntityEffects;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Checks the mutated mob against a nested condition.
/// If this condition's target is not a mutation entity it always returns false.
/// </summary>
public sealed partial class MutatedNestedCondition : TraumaEntityCondition<MutatedNestedCondition>
{
    [DataField("condition", required: true)]
    public EntityEffectCondition Nested = default!;

    public override string GuidebookExplanation(IPrototypeManager prototype)
        => Nested.GuidebookExplanation(prototype);
}

/// <summary>
/// Handles <see cref="MutatedNestedCondition"/>.
/// </summary>
public sealed partial class MutatedNestedConditionSystem : TraumaEntityConditionSystem<MutationComponent, MutatedNestedCondition>
{
    [Dependency] private TraumaEntityEffectsSystem _effects = default!;

    protected override bool Condition(Entity<MutationComponent> ent, MutatedNestedCondition condition)
        => ent.Comp.Target is { } target && _effects.TryCondition(target, condition.Nested);
}
