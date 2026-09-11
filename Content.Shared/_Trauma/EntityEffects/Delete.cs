// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Deletes the target entity, be careful with this...
/// </summary>
public sealed partial class Delete : EventEntityEffect<Delete>
{
    [DataField]
    public bool Queued;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class DeleteEffectSystem : TraumaEntityEffectSystem<MetaDataComponent, Delete>
{
    protected override void Effect(Entity<MetaDataComponent> ent, Delete effect, EntityEffectBaseArgs args)
    {
        if (TerminatingOrDeleted(ent, ent.Comp))
            return;

        if (effect.Queued)
            PredictedQueueDel(ent.Owner);
        else
            PredictedDel(ent.Owner);
    }
}
