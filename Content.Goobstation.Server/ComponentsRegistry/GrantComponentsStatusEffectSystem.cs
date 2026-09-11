// SPDX-FileCopyrightText: 2025 Goob Station Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusEffectNew;

namespace Content.Goobstation.Server.ComponentsRegistry;
public sealed partial class GrantComponentsStatusEffectSystem : EntitySystem
{
    // please don't use it for anything more complicated than adding immunity to stuff.
    // even so this could potentially break so much shit.

    // but it's more convenient than adding 100 bajillion of bloat status effects that inflate the project's filesize like a balloon

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrantComponentsStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusEffectApply);
        SubscribeLocalEvent<GrantComponentsStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusEffectRemove);
    }

    // Dumont
    private void OnStatusEffectApply(Entity<GrantComponentsStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        ent.Comp.Added.Clear();
        foreach (var name in ent.Comp.Components.Keys)
        {
            if (!HasComp(args.Target, Factory.GetRegistration(name).Type))
                ent.Comp.Added.Add(name);
        }

        EntityManager.AddComponents(args.Target, ent.Comp.Components, removeExisting: false);
    }

    private void OnStatusEffectRemove(Entity<GrantComponentsStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        foreach (var name in ent.Comp.Added)
        {
            RemComp(args.Target, Factory.GetRegistration(name).Type);
        }
        ent.Comp.Added.Clear();
    }
}
