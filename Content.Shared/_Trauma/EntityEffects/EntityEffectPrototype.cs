// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// A prototype for entity effects which can be reused via <see cref="NestedEffect"/>.
/// </summary>
/// <remarks>
/// </remarks>
[Prototype]
public sealed partial class EntityEffectPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// The effects of this prototype, applied in order.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    /// <summary>
    /// Conditions checked for this effect, regardless of the <see cref="NestedEffect"/> using it.
    /// </summary>
    [DataField]
    public EntityEffectCondition[]? Conditions;

    /// <summary>
    /// An override for the effect guidebook text, has "chance" passed from 0 to 1.
    /// By default one is generated from each effect.
    /// </summary>
    [DataField]
    public LocId? GuidebookText;
}

/// <summary>
/// A prototype for entity conditions which can be reused via <c>NestedCondition</c>.
/// </summary>
[Prototype]
public sealed partial class EntityConditionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// The condition of this prototype.
    /// </summary>
    [DataField(required: true)]
    public EntityEffectCondition Condition = default!;
}
