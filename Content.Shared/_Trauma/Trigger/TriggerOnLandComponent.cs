// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Trigger.Triggers;

/// <summary>
/// Triggers when the entity exits a floating or thrown state and lands on a surface.
/// The user is the thrower.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnLandComponent : BaseTriggerOnXComponent;
