// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Removes components from the target entity.
/// </summary>
public sealed partial class RemoveComponents : EventEntityEffect<RemoveComponents>
{
    /// <summary>
    /// Components to remove.
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    /// <summary>
    /// Text to use for the guidebook entry for reagents.
    /// </summary>
    [DataField]
    public LocId? GuidebookText;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => GuidebookText is { } loc ? Loc.GetString(loc, ("chance", Probability)) : null;
}

public sealed partial class RemoveComponentsEffectSystem : TraumaEntityEffectSystem<MetaDataComponent, RemoveComponents>
{
    protected override void Effect(Entity<MetaDataComponent> ent, RemoveComponents effect, EntityEffectBaseArgs args)
    {
        EntityManager.RemoveComponents(ent, effect.Components);
    }
}
