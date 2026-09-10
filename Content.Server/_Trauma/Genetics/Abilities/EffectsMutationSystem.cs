// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Trauma.Shared.Genetics.Abilities;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Random;

namespace Content.Trauma.Server.Genetics.Abilities;

/// <summary>
/// Handles running effects for <see cref="EffectsMutationComponent"/>.
/// </summary>
public sealed partial class EffectsMutationSystem : SharedEffectsMutationSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedEntityEffectSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnAdded(Entity<EffectsMutationComponent> ent, ref MutationAddedEvent args)
    {
        if (args.Automatic && ent.Comp.IgnoreAutomatic)
            return;

        ApplyEffects(args.Target, ent.Comp.Added);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<EffectsMutationComponent> ent, ref MutationRemovedEvent args)
    {
        if (args.Automatic && ent.Comp.IgnoreAutomatic)
            return;

        ApplyEffects(args.Target, ent.Comp.Removed);
    }

    private void ApplyEffects(EntityUid target, EntityEffect[] effects)
    {
        foreach (var effect in effects)
        {
            var args = new EntityEffectBaseArgs(target, EntityManager);
            if (!effect.ShouldApply(args, _random))
                continue;

            _effects.Effect(effect, args);
        }
    }
}
