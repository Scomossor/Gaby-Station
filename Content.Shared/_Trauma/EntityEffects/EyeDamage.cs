// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Modifies eye damage by a given amount, modified by reagent scale if any, floored to an integer.
/// </summary>
public sealed partial class EyeDamage : EventEntityEffect<EyeDamage>
{
    /// <summary>
    /// The amount of eye damage we're adding or removing.
    /// </summary>
    [DataField]
    public int Amount = -1;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class EyeDamageEffectSystem : TraumaEntityEffectSystem<MetaDataComponent, EyeDamage>
{
    [Dependency] private BlindableSystem _blindable = default!;

    protected override void Effect(Entity<MetaDataComponent> ent, EyeDamage effect, EntityEffectBaseArgs args)
    {
        var scale = args is EntityEffectReagentArgs reagent ? reagent.Scale.Float() : 1f;
        var amount = (int) Math.Floor(effect.Amount * scale);
        _blindable.AdjustEyeDamage(ent.Owner, amount);
    }
}
