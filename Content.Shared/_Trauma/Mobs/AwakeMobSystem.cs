// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Bed.Sleep;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Network;

namespace Content.Trauma.Shared.Mobs;

public sealed partial class AwakeMobSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MobStateChangedEvent>(OnStateChanged);
        SubscribeLocalEvent<SleepingComponent, ComponentStartup>(OnFellAsleep);
        SubscribeLocalEvent<SleepingComponent, ComponentShutdown>(OnWokeUp);
    }

    private void OnMapInit(Entity<MobStateComponent> ent, ref MapInitEvent args)
    {
        Refresh(ent, ent.Comp.CurrentState);
    }

    private void OnStateChanged(MobStateChangedEvent args)
    {
        Refresh(args.Target, args.NewMobState);
    }

    private void OnFellAsleep(Entity<SleepingComponent> ent, ref ComponentStartup args)
    {
        if (_net.IsClient)
            return;

        RemComp<AwakeMobComponent>(ent);
    }

    private void OnWokeUp(Entity<SleepingComponent> ent, ref ComponentShutdown args)
    {
        if (_net.IsClient || TerminatingOrDeleted(ent))
            return;

        if (TryComp<MobStateComponent>(ent, out var mob) && mob.CurrentState == MobState.Alive)
            EnsureComp<AwakeMobComponent>(ent);
    }

    private void Refresh(EntityUid uid, MobState state)
    {
        if (_net.IsClient || TerminatingOrDeleted(uid))
            return;

        if (state == MobState.Alive && !HasComp<SleepingComponent>(uid))
            EnsureComp<AwakeMobComponent>(uid);
        else
            RemComp<AwakeMobComponent>(uid);
    }
}
