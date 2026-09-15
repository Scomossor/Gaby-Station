// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions.Events;
using Content.Trauma.Shared.EntityEffects;
using Robust.Shared.Network;

namespace Content.Trauma.Shared.Actions;

public sealed partial class EffectActionSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private TraumaEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EffectActionComponent, ActionPerformedEvent>(OnActionPerformed);
        SubscribeLocalEvent<EffectActionComponent, EffectInstantActionEvent>(OnInstantAction);
        SubscribeLocalEvent<EffectActionComponent, EffectTargetActionEvent>(OnTargetAction);
    }

    private void OnActionPerformed(Entity<EffectActionComponent> ent, ref ActionPerformedEvent args)
    {
        if (ent.Comp.OnPerformed && _net.IsServer)
            _effects.ApplyEffects(args.Performer, ent.Comp.Effects);
    }

    private void OnInstantAction(Entity<EffectActionComponent> ent, ref EffectInstantActionEvent args)
    {
        if (_net.IsServer)
            _effects.ApplyEffects(args.Performer, ent.Comp.Effects);
        args.Handled = true;
    }

    private void OnTargetAction(Entity<EffectActionComponent> ent, ref EffectTargetActionEvent args)
    {
        if (_net.IsServer)
            _effects.ApplyEffects(args.Target, ent.Comp.Effects);
        args.Handled = true;
    }
}
