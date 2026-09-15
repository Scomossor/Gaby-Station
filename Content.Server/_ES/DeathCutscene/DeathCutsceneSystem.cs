// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Ghost;
using Content.Shared._ES.DeathCutscene;
using Content.Shared.Body.Events;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._ES.DeathCutscene;

public sealed class DeathCutsceneSystem : EntitySystem
{
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeathCutsceneComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ActiveDeathCutsceneComponent, BeingGibbedEvent>(OnBeingGibbed);
        SubscribeLocalEvent<ActiveDeathCutsceneComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveDeathCutsceneComponent>();
        while (query.MoveNext(out var uid, out var active))
        {
            if (_timing.CurTime < active.GhostTime)
                continue;

            GhostPlayer((uid, active));
        }
    }

    private void OnMobStateChanged(Entity<DeathCutsceneComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            StartCutscene(ent);
        else if (args.OldMobState == MobState.Dead)
            StopCutscene(ent.Owner);
    }

    private void OnBeingGibbed(Entity<ActiveDeathCutsceneComponent> ent, ref BeingGibbedEvent args)
    {
        if (!TryComp<ActorComponent>(ent, out var actor) || !_mind.TryGetMind(ent.Owner, out var mindId, out _))
            return;

        if (!TryComp<DeathCutsceneComponent>(ent, out var cutscene))
            return;

        var eye = Spawn(cutscene.EyePrototype, _transform.GetMapCoordinates(ent.Owner));
        _metaData.SetEntityName(eye, Loc.GetString("death-cutscene-eye-name", ("name", Name(ent.Owner))));

        var active = ent.Comp;
        RemComp<ActiveDeathCutsceneComponent>(ent);

        _mind.TransferTo(mindId, eye);
        _player.SetAttachedEntity(actor.PlayerSession, eye);

        var eyeActive = EnsureComp<ActiveDeathCutsceneComponent>(eye);
        eyeActive.GhostTime = active.GhostTime;
        eyeActive.CanReturnToBody = active.CanReturnToBody;
    }

    private void OnPlayerDetached(Entity<ActiveDeathCutsceneComponent> ent, ref PlayerDetachedEvent args)
    {
        RemCompDeferred<ActiveDeathCutsceneComponent>(ent);
        StopClientCutscene(args.Player);
    }

    private void StartCutscene(Entity<DeathCutsceneComponent> ent)
    {
        if (HasComp<ActiveDeathCutsceneComponent>(ent))
            return;

        if (!TryComp<ActorComponent>(ent, out var actor) || !_mind.TryGetMind(ent.Owner, out _, out _))
            return;

        var timings = ent.Comp.GetTimings();

        var active = EnsureComp<ActiveDeathCutsceneComponent>(ent);
        active.GhostTime = _timing.CurTime + timings.GhostDelay;
        active.CanReturnToBody = ent.Comp.CanReturnToBody;

        RaiseNetworkEvent(new PlayDeathCutsceneEvent(timings, ent.Comp.Sound, ent.Comp.SuppressAmbientMusic),
            actor.PlayerSession);
    }

    private void StopCutscene(EntityUid uid)
    {
        if (!HasComp<ActiveDeathCutsceneComponent>(uid))
            return;

        RemCompDeferred<ActiveDeathCutsceneComponent>(uid);

        if (TryComp<ActorComponent>(uid, out var actor))
            StopClientCutscene(actor.PlayerSession);
    }

    private void StopClientCutscene(ICommonSession session)
    {
        if (session.Channel is not { IsConnected: true })
            return;

        RaiseNetworkEvent(new StopDeathCutsceneEvent(), session);
    }

    private void GhostPlayer(Entity<ActiveDeathCutsceneComponent> ent)
    {
        var canReturn = ent.Comp.CanReturnToBody;
        var isEye = HasComp<DeathCutsceneEyeComponent>(ent);
        TryComp<ActorComponent>(ent, out var actor);

        RemComp<ActiveDeathCutsceneComponent>(ent);

        var ghosted = _mind.TryGetMind(ent.Owner, out var mindId, out var mind)
                      && _ghost.OnGhostAttempt(mindId, canReturn, mind: mind);

        if (!ghosted && actor != null)
            StopClientCutscene(actor.PlayerSession);

        if (isEye)
            QueueDel(ent.Owner);
    }
}
