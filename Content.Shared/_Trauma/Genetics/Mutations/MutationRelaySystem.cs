// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Flash;
using Content.Shared.Mobs;
using Content.Shared.Speech;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Trauma.Shared.Genetics.Mutations;

/// <summary>
/// Relays some events from the mutated mob to the mutation entities.
/// </summary>
public sealed class MutationRelaySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MutatableComponent, AfterFlashedEvent>(RelayEvent);
        SubscribeLocalEvent<MutatableComponent, MobStateChangedEvent>(RelayEvent);
        SubscribeLocalEvent<MutatableComponent, BleedModifierEvent>(RelayEvent);
        SubscribeLocalEvent<MutatableComponent, DamageModifyEvent>(RelayEvent);
        SubscribeLocalEvent<MutatableComponent, GetUserMeleeDamageEvent>(RelayEvent);
        SubscribeLocalEvent<MutatableComponent, AccentGetEvent>(RelayEvent);
    }

    public void RelayEvent<T>(Entity<MutatableComponent> ent, ref T args) where T: notnull
    {
        foreach (var uid in ent.Comp.Mutations.Values)
        {
            RaiseLocalEvent(uid, ref args);
        }
    }
}
