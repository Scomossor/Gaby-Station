// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Uses the condition of an <see cref="EntityConditionPrototype"/>.
/// </summary>
public sealed partial class NestedCondition : TraumaEntityCondition<NestedCondition>
{
    /// <summary>
    /// The condition prototype to use.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<EntityConditionPrototype> Proto;

    public override string GuidebookExplanation(IPrototypeManager prototype)
        => prototype.Index(Proto).Condition.GuidebookExplanation(prototype);
}

/// <summary>
/// Handles <see cref="NestedCondition"/>.
/// </summary>
public sealed partial class NestedConditionSystem : TraumaEntityConditionSystem<TransformComponent, NestedCondition>
{
    [Dependency] private TraumaEntityEffectsSystem _effects = default!;

    protected override bool Condition(Entity<TransformComponent> ent, NestedCondition condition)
        => _effects.TryCondition(ent, condition.Proto);
}
