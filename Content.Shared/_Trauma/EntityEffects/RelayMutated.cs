// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// For a mutation target, relays an effect to the target mob.
/// The relayed effect checks its own probability and conditions against the mob.
/// </summary>
public sealed partial class RelayMutated : EventEntityEffect<RelayMutated>
{
    /// <summary>
    /// Effect to apply to the mutated mob.
    /// </summary>
    /// <remarks>
    /// </remarks>
    [DataField("effect", required: true)]
    public EntityEffect Relayed = default!;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Relayed.GuidebookEffectDescription(prototype, entSys);
}

/// <summary>
/// Handles <see cref="RelayMutated"/>.
/// </summary>
public sealed partial class RelayMutatedEffectSystem : TraumaEntityEffectSystem<MutationComponent, RelayMutated>
{
    [Dependency] private TraumaEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<MutationComponent> ent, RelayMutated effect, EntityEffectBaseArgs args)
    {
        if (ent.Comp.Target is { } mob)
            _effects.TryApplyEffect(mob, effect.Relayed);
    }
}
