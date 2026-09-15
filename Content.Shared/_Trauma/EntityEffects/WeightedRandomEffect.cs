// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Like <c>WeightedRandomPrototype</c> but for <see cref="EntityEffect"/>.
/// When ran it will activate one random child effect, which then checks its own probability and conditions.
/// </summary>
public sealed partial class WeightedRandomEffect : EventEntityEffect<WeightedRandomEffect>
{
    [DataField(required: true)]
    public List<WeightedEffect> Children = new();

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public float GetTotalWeights()
    {
        var total = 0f;
        foreach (var child in Children)
        {
            total += child.Weight;
        }
        return total;
    }
}

public sealed partial class WeightedRandomEffectSystem : TraumaEntityEffectSystem<MetaDataComponent, WeightedRandomEffect>
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private TraumaEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<MetaDataComponent> ent, WeightedRandomEffect effect, EntityEffectBaseArgs args)
    {
        var total = 0f;
        var target = _random.NextFloat() * effect.GetTotalWeights();
        foreach (var child in effect.Children)
        {
            total += child.Weight;
            if (total >= target)
            {
                _effects.TryApplyEffect(ent, child.Effect);
                return;
            }
        }
    }
}

[DataDefinition]
public partial record struct WeightedEffect()
{
    [DataField(required: true)]
    public EntityEffect Effect = default!;

    // see RT#6556 for why this cant be a single line struct
    [DataField]
    public float Weight = 1f;
}
