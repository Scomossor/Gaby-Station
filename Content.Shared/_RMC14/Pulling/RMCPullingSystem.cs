﻿using System.Numerics;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Coordinates;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Pulling;

public sealed class RMCPullingSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedXenoParasiteSystem _parasite = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly SoundSpecifier PullSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.05f)
    };

    private const string PullEffect = "CMEffectGrab";

    private EntityQuery<PreventPulledWhileAliveComponent> _preventPulledWhileAliveQuery;

    public override void Initialize()
    {
        _preventPulledWhileAliveQuery = GetEntityQuery<PreventPulledWhileAliveComponent>();

        SubscribeLocalEvent<ParalyzeOnPullAttemptComponent, PullAttemptEvent>(OnParalyzeOnPullAttempt);
        SubscribeLocalEvent<InfectOnPullAttemptComponent, PullAttemptEvent>(OnInfectOnPullAttempt);

        SubscribeLocalEvent<SlowOnPullComponent, PullStartedMessage>(OnSlowPullStarted);
        SubscribeLocalEvent<SlowOnPullComponent, PullStoppedMessage>(OnSlowPullStopped);

        SubscribeLocalEvent<PullingSlowedComponent, RefreshMovementSpeedModifiersEvent>(OnPullingSlowedMovementSpeed);

        SubscribeLocalEvent<PullWhitelistComponent, PullAttemptEvent>(OnPullWhitelistPullAttempt);

        SubscribeLocalEvent<BlockPullingDeadComponent, PullAttemptEvent>(OnBlockDeadPullAttempt);
        SubscribeLocalEvent<BlockPullingDeadComponent, PullStartedMessage>(OnBlockDeadPullStarted);
        SubscribeLocalEvent<BlockPullingDeadComponent, PullStoppedMessage>(OnBlockDeadPullStopped);

        SubscribeLocalEvent<PreventPulledWhileAliveComponent, PullAttemptEvent>(OnPreventPulledWhileAliveAttempt);
        SubscribeLocalEvent<PreventPulledWhileAliveComponent, PullStartedMessage>(OnPreventPulledWhileAliveStart);
        SubscribeLocalEvent<PreventPulledWhileAliveComponent, PullStoppedMessage>(OnPreventPulledWhileAliveStop);

        SubscribeLocalEvent<PullableComponent, PullStartedMessage>(OnPullAnimation);
    }

    private void OnParalyzeOnPullAttempt(Entity<ParalyzeOnPullAttemptComponent> ent, ref PullAttemptEvent args)
    {
        var user = args.PullerUid;
        var target = args.PulledUid;
        if (target != ent.Owner ||
            HasComp<ParalyzeOnPullAttemptImmuneComponent>(user) ||
            _mobState.IsDead(ent))
        {
            return;
        }

        args.Cancelled = true;

        if (ent.Comp.Sound is { } sound)
        {
            var pitch = _random.NextFloat(ent.Comp.MinPitch, ent.Comp.MaxPitch);
            _audio.PlayPredicted(sound, ent, user, sound.Params.WithPitchScale(pitch));
        }

        _stun.TryParalyze(user, ent.Comp.Duration, true);

        var puller = user;
        var pulled = target;
        var othersMessage = Loc.GetString("rmc-pull-paralyze-others", ("puller", puller), ("pulled", pulled));
        var selfMessage = Loc.GetString("rmc-pull-paralyze-self", ("puller", puller), ("pulled", pulled));

        _popup.PopupPredicted(selfMessage, othersMessage, puller, puller, PopupType.MediumCaution);
    }

    private void OnInfectOnPullAttempt(Entity<InfectOnPullAttemptComponent> ent, ref PullAttemptEvent args)
    {
        var user = args.PullerUid;
        var target = args.PulledUid;
        if (target != ent.Owner ||
            HasComp<InfectOnPullAttemptImmuneComponent>(user) ||
            _mobState.IsDead(ent))
        {
            return;
        }

        if (!TryComp<XenoParasiteComponent>(target, out var paraComp))
            return;

        Entity<XenoParasiteComponent> comp = (target, paraComp);
        args.Cancelled = true;

        if (!_parasite.Infect(comp, user, false, true))
            return;

        var puller = user;
        var pulled = target;
        var othersMessage = Loc.GetString("rmc-pull-infect-others", ("puller", puller), ("pulled", pulled));
        var selfMessage = Loc.GetString("rmc-pull-infect-self", ("puller", puller), ("pulled", pulled));

        _popup.PopupPredicted(selfMessage, othersMessage, puller, puller, PopupType.MediumCaution);
    }

    private void OnSlowPullStarted(Entity<SlowOnPullComponent> ent, ref PullStartedMessage args)
    {
        if (ent.Owner == args.PullerUid)
        {
            EnsureComp<PullingSlowedComponent>(args.PullerUid);
            _movementSpeed.RefreshMovementSpeedModifiers(args.PullerUid);
        }
    }

    private void OnSlowPullStopped(Entity<SlowOnPullComponent> ent, ref PullStoppedMessage args)
    {
        if (ent.Owner == args.PullerUid)
        {
            RemComp<PullingSlowedComponent>(args.PullerUid);
            _movementSpeed.RefreshMovementSpeedModifiers(args.PullerUid);
        }
    }

    private void OnPullingSlowedMovementSpeed(Entity<PullingSlowedComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (HasComp<BypassInteractionChecksComponent>(ent) ||
            !TryComp(ent, out PullerComponent? puller) ||
            !TryComp(ent, out SlowOnPullComponent? slow))
        {
            return;
        }

        if (puller.Pulling == null)
            return;

        foreach (var slowdown in slow.Slowdowns)
        {
            if (_whitelist.IsWhitelistPass(slowdown.Whitelist, puller.Pulling.Value))
            {
                args.ModifySpeed(slowdown.Multiplier, slowdown.Multiplier);
                return;
            }
        }

        args.ModifySpeed(slow.Multiplier, slow.Multiplier);
    }

    private void OnPullWhitelistPullAttempt(Entity<PullWhitelistComponent> ent, ref PullAttemptEvent args)
    {
        if (args.Cancelled || ent.Owner == args.PulledUid)
            return;

        if (!_whitelist.IsValid(ent.Comp.Whitelist, args.PulledUid))
        {
            _popup.PopupClient(Loc.GetString("cm-pull-whitelist-denied", ("name", args.PulledUid)), args.PulledUid, args.PullerUid);
            args.Cancelled = true;
        }
    }

    private void OnBlockDeadPullAttempt(Entity<BlockPullingDeadComponent> ent, ref PullAttemptEvent args)
    {
        if (args.Cancelled || ent.Owner == args.PulledUid)
            return;

        if (_mobState.IsDead(args.PulledUid))
        {
            _popup.PopupClient(Loc.GetString("cm-pull-whitelist-denied-dead", ("name", args.PulledUid)), args.PulledUid, args.PullerUid);
            args.Cancelled = true;
        }
    }

    private void OnBlockDeadPullStarted(Entity<BlockPullingDeadComponent> ent, ref PullStartedMessage args)
    {
        if (ent.Owner == args.PullerUid)
            EnsureComp<BlockPullingDeadActiveComponent>(ent);
    }

    private void OnBlockDeadPullStopped(Entity<BlockPullingDeadComponent> ent, ref PullStoppedMessage args)
    {
        if (ent.Owner == args.PullerUid)
            RemCompDeferred<BlockPullingDeadActiveComponent>(ent);
    }

    private void OnPreventPulledWhileAliveAttempt(Entity<PreventPulledWhileAliveComponent> ent, ref PullAttemptEvent args)
    {
        if (args.PulledUid != ent.Owner)
            return;

        if (!CanPullPreventPulledWhileAlive((ent, ent), args.PullerUid))
        {
            var msg = Loc.GetString("rmc-prevent-pull-alive", ("target", ent));
            _popup.PopupClient(msg, ent, args.PullerUid, PopupType.SmallCaution);
            args.Cancelled = true;
        }
    }

    private void OnPreventPulledWhileAliveStart(Entity<PreventPulledWhileAliveComponent> ent, ref PullStartedMessage args)
    {
        if (args.PulledUid != ent.Owner)
            return;

        EnsureComp<ActivePreventPulledWhileAliveComponent>(ent);
    }

    private void OnPreventPulledWhileAliveStop(Entity<PreventPulledWhileAliveComponent> ent, ref PullStoppedMessage args)
    {
        if (args.PulledUid != ent.Owner)
            return;

        RemCompDeferred<ActivePreventPulledWhileAliveComponent>(ent);
    }

    private bool CanPullPreventPulledWhileAlive(Entity<PreventPulledWhileAliveComponent?> pulled, EntityUid user)
    {
        if (!Resolve(pulled, ref pulled.Comp, false))
            return true;

        if (!_mobState.IsAlive(pulled))
            return true;

        if (!_whitelist.IsWhitelistPassOrNull(pulled.Comp.Whitelist, user))
            return true;

        foreach (var effect in pulled.Comp.ExceptEffects)
        {
            if (_statusEffects.HasStatusEffect(pulled, effect))
                return true;
        }

        return false;
    }

    public void TryStopUserPullIfPulling(EntityUid user, EntityUid target)
    {
        if (!TryComp(user, out PullerComponent? puller) ||
            puller.Pulling != target ||
            !TryComp(puller.Pulling, out PullableComponent? pullable))
        {
            return;
        }

        _pulling.TryStopPull(puller.Pulling.Value, pullable, user);
    }

    private void OnPullAnimation(Entity<PullableComponent> ent, ref PullStartedMessage args)
    {
        if (args.PulledUid != ent.Owner)
            return;

        var pulled = args.PulledUid;
        var puller = args.PullerUid;

        var userXform = Transform(puller);
        var targetPos = _transform.GetWorldPosition(pulled);
        var localPos = Vector2.Transform(targetPos, _transform.GetInvWorldMatrix(userXform));
        localPos = userXform.LocalRotation.RotateVec(localPos);

        _melee.DoLunge(puller, puller, Angle.Zero, localPos, null);
        _audio.PlayPredicted(PullSound, pulled, puller);

        if (_net.IsServer)
            SpawnAttachedTo(PullEffect, pulled.ToCoordinates());
    }

    public override void Update(float frameTime)
    {
        var blockDeadActive = EntityQueryEnumerator<BlockPullingDeadActiveComponent, PullerComponent>();
        while (blockDeadActive.MoveNext(out var uid, out _, out var puller))
        {
            if (puller.Pulling is not { } pulling ||
                !TryComp(pulling, out PullableComponent? pullable))
            {
                continue;
            }

            if (_mobState.IsDead(pulling))
                _pulling.TryStopPull(pulling, pullable, uid);
        }

        var preventPulledWhileAlive = EntityQueryEnumerator<ActivePreventPulledWhileAliveComponent, PreventPulledWhileAliveComponent, PullableComponent>();
        while (preventPulledWhileAlive.MoveNext(out var uid, out _, out var prevent, out var pullable))
        {
            if (pullable.Puller is not { } puller ||
                CanPullPreventPulledWhileAlive((uid, prevent), puller))
            {
                continue;
            }

            _pulling.TryStopPull(uid, pullable);
        }
    }
}