// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Trigger;

/// <summary>
/// Applies a list of entity effects to the owning entity when triggered.
/// If TargetUser is true then they will be applied to the user instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EntityEffectOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The effects to apply, in order.
    /// </summary>
    [DataField] // unnetworked because EntityEffect isnt serializable
    public EntityEffect[] Effects = [];

    /// <summary>
    /// Optional scale multiplier for the effects.
    /// </summary>
    /// <remarks>
    /// </remarks>
    [DataField, AutoNetworkedField]
    public float Scale = 1f;
}
