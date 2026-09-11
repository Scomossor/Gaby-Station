// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Adds a marking to the target humanoid, colored the same way the add-marking surgery does.
/// </summary>
public sealed partial class AddMarking : EventEntityEffect<AddMarking>
{
    [DataField(required: true)]
    public ProtoId<MarkingPrototype> Marking;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

/// <summary>
/// Removes a marking from the target humanoid.
/// </summary>
public sealed partial class RemoveMarking : EventEntityEffect<RemoveMarking>
{
    [DataField(required: true)]
    public ProtoId<MarkingPrototype> Marking;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}
