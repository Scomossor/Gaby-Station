// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Trigger.Components.Conditions;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Trigger;

/// <summary>
/// Uses an <see cref="EntityEffectCondition"/> as a trigger condition.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GenericTriggerConditionComponent : BaseTriggerConditionComponent
{
    [DataField(required: true)]
    public EntityEffectCondition Condition = default!;

    /// <summary>
    /// If true, checks the condition against the user instead of this entity.
    /// If there is no user for a trigger attempt, triggering will be prevented.
    /// </summary>
    [DataField]
    public bool CheckUser;
}
