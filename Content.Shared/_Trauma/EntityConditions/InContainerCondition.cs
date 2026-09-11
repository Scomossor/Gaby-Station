// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Requires that the target entity is inside of any container.
/// </summary>
public sealed partial class InContainerCondition : TraumaEntityCondition<InContainerCondition>
{
    public override string GuidebookExplanation(IPrototypeManager prototype)
        => string.Empty;
}

/// <summary>
/// Handles <see cref="InContainerCondition"/>.
/// </summary>
public sealed partial class InContainerConditionSystem : TraumaEntityConditionSystem<MetaDataComponent, InContainerCondition>
{
    [Dependency] private SharedContainerSystem _container = default!;

    protected override bool Condition(Entity<MetaDataComponent> ent, InContainerCondition condition)
        => _container.TryGetContainingContainer((ent, null, ent.Comp), out _);
}
