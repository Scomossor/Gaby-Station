// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Applies the effect of an <see cref="EntityEffectPrototype"/>.
/// The prototype's own conditions are checked against the same target before its effects run.
/// </summary>
public sealed partial class NestedEffect : EventEntityEffect<NestedEffect>
{
    /// <summary>
    /// The effect prototype to use.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<EntityEffectPrototype> Proto;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var proto = prototype.Index(Proto);
        if (proto.GuidebookText is { } key)
            return Loc.GetString(key, ("chance", Probability));

        var texts = new List<string>();
        foreach (var effect in proto.Effects)
        {
            if (effect.GuidebookEffectDescription(prototype, entSys) is { } text)
                texts.Add(text);
        }

        return texts.Count == 0 ? null : string.Join("\n", texts);
    }
}

/// <summary>
/// Handles <see cref="NestedEffect"/>.
/// </summary>
public sealed partial class NestedEffectSystem : TraumaEntityEffectSystem<TransformComponent, NestedEffect>
{
    [Dependency] private TraumaEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<TransformComponent> ent, NestedEffect effect, EntityEffectBaseArgs args)
    {
        _effects.TryApplyEffect(ent, effect.Proto);
    }
}
