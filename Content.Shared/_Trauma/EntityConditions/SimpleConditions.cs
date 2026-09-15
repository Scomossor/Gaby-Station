// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Standing;
using Content.Shared.Timing;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Requires that the target entity is standing and not downed/crawling.
/// Always fails for entities that don't have <see cref="StandingStateComponent"/>.
/// </summary>
public sealed partial class StandingCondition : TraumaEntityCondition<StandingCondition>
{
    public override string GuidebookExplanation(IPrototypeManager prototype)
        => string.Empty;
}

public sealed partial class StandingConditionSystem : TraumaEntityConditionSystem<StandingStateComponent, StandingCondition>
{
    protected override bool Condition(Entity<StandingStateComponent> ent, StandingCondition condition)
        => ent.Comp.Standing;
}

/// <summary>
/// Condition that requires a use delay to not be active.
/// </summary>
public sealed partial class UseDelayCondition : TraumaEntityCondition<UseDelayCondition>
{
    /// <summary>
    /// The specific use delay to check.
    /// </summary>
    [DataField]
    public string DelayId = UseDelaySystem.DefaultId;

    public override string GuidebookExplanation(IPrototypeManager prototype)
        => string.Empty;
}

public sealed partial class UseDelayConditionSystem : TraumaEntityConditionSystem<UseDelayComponent, UseDelayCondition>
{
    [Dependency] private UseDelaySystem _useDelay = default!;

    protected override bool Condition(Entity<UseDelayComponent> ent, UseDelayCondition condition)
        => !_useDelay.IsDelayed((ent, ent.Comp), condition.DelayId);
}

/// <summary>
/// Requires that the target entity is holding an item.
/// </summary>
public sealed partial class HoldingItemCondition : TraumaEntityCondition<HoldingItemCondition>
{
    public override string GuidebookExplanation(IPrototypeManager prototype)
        => string.Empty;
}

public sealed partial class HoldingItemConditionSystem : TraumaEntityConditionSystem<HandsComponent, HoldingItemCondition>
{
    [Dependency] private SharedHandsSystem _hands = default!;

    protected override bool Condition(Entity<HandsComponent> ent, HoldingItemCondition condition)
    {
        foreach (var _ in _hands.EnumerateHeld((ent, ent.Comp)))
        {
            return true;
        }

        return false;
    }
}
