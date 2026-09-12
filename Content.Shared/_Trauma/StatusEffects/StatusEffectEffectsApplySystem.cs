// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusEffectNew;
using Content.Trauma.Shared.EntityEffects;

namespace Content.Trauma.Shared.StatusEffects;

public sealed class StatusEffectEffectsApplySystem : EntitySystem
{
    [Dependency] private TraumaEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectEffectsApplyComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<StatusEffectEffectsApplyComponent, StatusEffectRemovedEvent>(OnRemoval);
    }

    private void OnApplied(Entity<StatusEffectEffectsApplyComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (ent.Comp.EffectsOnApply is not { } aoEntrar)
            return;

        _effects.ApplyEffects(args.Target, aoEntrar);
    }

    private void OnRemoval(Entity<StatusEffectEffectsApplyComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (ent.Comp.EffectsOnRemoval is not { } aoSair || TerminatingOrDeleted(args.Target))
            return;

        _effects.ApplyEffects(args.Target, aoSair);
    }
}
