// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Dataset;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Revert the target entity's polymorph.
/// </summary>
public sealed partial class RevertPolymorph : EventEntityEffect<RevertPolymorph>
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

/// <summary>
/// Makes the target entity say a random line from a localized dataset.
/// It can also have a string prefixed.
/// </summary>
public sealed partial class Speak : EventEntityEffect<Speak>
{
    [DataField(required: true)]
    public ProtoId<LocalizedDatasetPrototype> Id;

    [DataField]
    public LocId? Prefix;

    [DataField]
    public bool HideChat;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

/// <summary>
/// Adds a number of fire stacks to this entity, scaled by reagent quantity if any.
/// </summary>
public sealed partial class Flammable : EventEntityEffect<Flammable>
{
    /// <summary>
    /// Fire stack multiplier applied on an entity,
    /// unless that entity is already on fire and <see cref="MultiplierOnExisting"/> is not null.
    /// </summary>
    [DataField]
    public float Multiplier = 0.05f;

    /// <summary>
    /// Fire stack multiplier applied if the entity is already on fire. Defaults to <see cref="Multiplier"/> if null.
    /// </summary>
    [DataField]
    public float? MultiplierOnExisting;

    /// <summary>
    /// How much fire protection is mitigated (e.g. atmos firesuit+helmet is 1.0) when adding firestacks.
    /// </summary>
    [DataField]
    public float FireProtectionPenetration;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}
