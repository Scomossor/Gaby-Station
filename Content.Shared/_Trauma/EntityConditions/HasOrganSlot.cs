// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Trauma.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Requires that the target mob has an organ slot in a body part of the given type.
/// </summary>
public sealed partial class HasOrganSlot : TraumaEntityCondition<HasOrganSlot>
{
    /// <summary>
    /// Category of the organ slot that must exist in a found body part.
    /// </summary>
    [DataField(required: true)]
    public string Organ = string.Empty;

    [DataField(required: true)]
    public TraumaBodyPartType PartType;

    [DataField]
    public BodyPartSymmetry? Symmetry;

    public override string GuidebookExplanation(IPrototypeManager prototype)
        => string.Empty;
}

public sealed partial class HasOrganSlotConditionSystem : TraumaEntityConditionSystem<BodyComponent, HasOrganSlot>
{
    [Dependency] private TraumaBodySystem _traumaBody = default!;

    protected override bool Condition(Entity<BodyComponent> ent, HasOrganSlot condition)
    {
        foreach (var part in _traumaBody.GetBodyParts(ent, condition.PartType, condition.Symmetry))
        {
            if (TryComp<BodyPartComponent>(part, out var comp) && _traumaBody.HasSlot((part, comp), condition.Organ))
                return true;
        }

        return false;
    }
}
