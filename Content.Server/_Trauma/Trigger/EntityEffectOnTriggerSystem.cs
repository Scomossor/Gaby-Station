// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Trigger;
using Content.Trauma.Shared.EntityEffects;
using Content.Trauma.Shared.Trigger;

namespace Content.Trauma.Server.Trigger;

/// <summary>
/// Applies <see cref="EntityEffectOnTriggerComponent"/> effects when triggered.
/// </summary>
/// <remarks>
/// </remarks>
public sealed partial class EntityEffectOnTriggerSystem : EntitySystem
{
    [Dependency] private TraumaEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityEffectOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<EntityEffectOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;
        if (target == null)
            return;

        _effects.ApplyEffects(target.Value, ent.Comp.Effects);
        args.Handled = true;
    }
}
