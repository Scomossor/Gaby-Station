// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// Marking the file for future changes

using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared._Gabystation.Interaction.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RecallableComponent : Component
{
    /// <summary>
    /// The entity that is currently linked to this one and can recall it. Null if not linked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? LinkedEntity;

    /// <summary>
    /// The action granted to an entity when it links to this one.
    /// </summary>
    /// <remarks>
    /// This action should allow the linked entity to recall (teleport) this entity.
    /// </remarks>
    [DataField]
    public EntProtoId RecallAction = "ActionRecallBoundItem";

    /// <summary>
    /// Allows the item to display binding info on examine.
    /// </summary>
    [DataField]
    public bool Examinable = true;
}
