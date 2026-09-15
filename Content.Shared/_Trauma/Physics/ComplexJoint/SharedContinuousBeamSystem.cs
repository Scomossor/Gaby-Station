// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Goobstation.Common.Physics;
using Content.Shared.CombatMode;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Physics.ComplexJoint;

public abstract class SharedContinuousBeamSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPvsOverrideSystem _pvs = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;

    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedTransformSystem Xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<ContinuousBeamPositionEvent>(OnGetPosition);
        SubscribeLocalEvent<ContinuousBeamGunComponent, AttemptMeleeEvent>(OnAttemptAttack);
        SubscribeLocalEvent<ContinuousBeamGunComponent, GotUnequippedHandEvent>(OnHandUnequip);
        SubscribeLocalEvent<ContinuousBeamEndpointComponent, ComponentShutdown>(OnEndpointShutdown);
    }

    private void OnGetPosition(ContinuousBeamPositionEvent ev, EntitySessionEventArgs args)
    {
        var pos = GetCoordinates(ev.Coordinates);

        if (!Exists(pos.EntityId) || args.SenderSession.AttachedEntity is not { } player)
            return;

        var uid = pos.EntityId;

        if (!TryComp(uid, out ContinuousBeamGunComponent? gun))
            return;

        var xform = Transform(uid);
        var newParent = xform.GridUid ?? xform.MapUid;
        if (newParent == null)
            return;

        Entity<ContinuousBeamGunComponent> gunEnt = (uid, gun);

        var len = pos.Position.Length();
        var newLen = Math.Clamp(len, gun.MinMaxLaserRange.X, gun.MinMaxLaserRange.Y);
        if (Math.Abs(len - newLen) > 0.01f)
            pos = new EntityCoordinates(uid, pos.Position * newLen / len);

        gun.ShootCoordinates = Xform.WithEntityId(pos, newParent.Value);

        if (ev.ShouldFire && ValidateGun(player, gunEnt) && CanFire(player, gunEnt))
            ShootLaser(player, gunEnt);
        else if (gun.UserCanFire)
            StopFiring(uid, gun, null);
    }

    public bool CanFire(EntityUid user, [NotNullWhen(true)] out Entity<ContinuousBeamGunComponent>? gun)
    {
        return TryGetGun(user, out gun) && CanFire(user, gun.Value);
    }

    public bool CanFire(EntityUid user, Entity<ContinuousBeamGunComponent> gun)
    {
        return gun.Comp.UserCanFire && _combat.IsInCombatMode(user);
    }

    public EntityUid? ShootLaser(EntityUid user, Entity<ContinuousBeamGunComponent> gun)
    {
        if (gun.Comp.ShootCoordinates is not { } coords)
            return null;

        if (Exists(gun.Comp.Endpoint))
            return gun.Comp.Endpoint.Value;

        var endpoint = EntityManager.PredictedSpawnAttachedTo(null, coords);
        var endpointComp = EnsureComp<ContinuousBeamEndpointComponent>(endpoint);
        endpointComp.Gun = gun;
        Dirty(endpoint, endpointComp);

        if (endpointComp.PvsOverride)
        {
            _pvs.AddGlobalOverride(endpoint);
            _pvs.AddGlobalOverride(gun);
        }

        if (gun.Comp.BeamTime > 0f)
            EnsureComp<TimedDespawnComponent>(endpoint).Lifetime = gun.Comp.BeamTime;

        var now = Timing.CurTime;

        gun.Comp.Data.CreationTime = now;
        CreateJoint(endpoint, gun, gun.Comp.Data);

        // Audio is server side only, stopping a predicted sound on the client is not supported.
        if (_net.IsServer)
        {
            if (Exists(gun.Comp.BeamSoundEnt))
                _audio.Stop(gun.Comp.BeamSoundEnt.Value);

            var filter = Filter.BroadcastMap(Transform(gun).MapID);
            gun.Comp.BeamSoundEnt = _audio.PlayEntity(gun.Comp.BeamSound, filter, gun, false)?.Entity;
        }

        gun.Comp.Endpoint = endpoint;
        gun.Comp.BeamTimer = now + TimeSpan.FromSeconds(gun.Comp.BeamTime);
        gun.Comp.Shooter = user;
        Dirty(gun);

        return endpoint;
    }

    public bool ResolveBeamEndpointData(EntityUid uid,
        ContinuousBeamGunComponent gun,
        ComplexJointVisualsComponent joint)
    {
        if (Exists(gun.Endpoint) && gun.ShootCoordinates is { } coords && coords.IsValid(EntityManager))
            return true;

        StopFiring(uid, gun, joint);
        return false;
    }

    public void StopFiring(EntityUid uid, ContinuousBeamGunComponent gun, ComplexJointVisualsComponent? joint)
    {
        ClearBeamJoints((uid, joint), gun.Data.Id);

        if (Exists(gun.Endpoint))
            PredictedQueueDel(gun.Endpoint.Value);

        gun.Endpoint = null;
        Dirty(uid, gun);
    }

    public (Box2Rotated boxRot, Angle angle, float distToEndpoint, Vector2 dir, Vector2 offset, Vector2 pos, Vector2
        endpointPos)?
        CalculateBeamDamageData(Entity<ContinuousBeamGunComponent, ComplexJointVisualsComponent> ent)
    {
        if (!ResolveBeamEndpointData(ent, ent, ent))
            return null;

        var xform = Transform(ent);

        var pos = Xform.GetWorldPosition(ent.Comp1.Endpoint!.Value);
        var ourPos = Xform.GetWorldPosition(xform);
        var c = pos - ourPos;

        var cLen = c.Length();

        if (cLen <= 0.01f)
            return null;

        var cNorm = c / cLen;
        var angle = c.ToAngle();

        var offset = cNorm * ent.Comp1.BeamScale;
        var box = new Box2(ourPos + offset + new Vector2(0f, -ent.Comp1.LaserThickness),
            ourPos + offset + new Vector2(cLen, ent.Comp1.LaserThickness));
        var boxRot = new Box2Rotated(box, angle, ourPos + offset);
        return (boxRot, angle, cLen, cNorm, offset, ourPos, pos);
    }

    public bool ValidateGun(EntityUid user, Entity<ContinuousBeamGunComponent> gun)
    {
        return user == gun.Owner || _hands.IsHolding(user, gun);
    }

    public bool TryGetGun(EntityUid user, [NotNullWhen(true)] out Entity<ContinuousBeamGunComponent>? gun)
    {
        gun = null;

        if (_hands.GetActiveItem(user) is { } held &&
            TryComp(held, out ContinuousBeamGunComponent? gunComp))
        {
            gun = (held, gunComp);
            return true;
        }

        if (!TryComp(user, out gunComp))
            return false;

        gun = (user, gunComp);
        return true;
    }

    protected void CreateJoint(EntityUid uidA, EntityUid uidB, ComplexJointVisualsData data)
    {
        var beam = EnsureComp<ComplexJointVisualsComponent>(uidB);
        beam.Data[GetNetEntity(uidA)] = data;
        Dirty(uidB, beam);
    }

    protected void ClearBeamJoints(Entity<ComplexJointVisualsComponent?> ent, string excludedId)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Data = ent.Comp.Data.Where(x => x.Value.Id != excludedId).ToDictionary();

        if (ent.Comp.Data.Count == 0)
            RemComp(ent.Owner, ent.Comp);
        else
            Dirty(ent.Owner, ent.Comp);
    }

    private void OnAttemptAttack(Entity<ContinuousBeamGunComponent> ent, ref AttemptMeleeEvent args)
    {
        if (Exists(ent.Comp.Endpoint))
            args.Cancelled = true;
    }

    private void OnHandUnequip(Entity<ContinuousBeamGunComponent> ent, ref GotUnequippedHandEvent args)
    {
        StopFiring(ent, ent, null);
    }

    protected virtual void OnEndpointShutdown(Entity<ContinuousBeamEndpointComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Gun is not { } gun)
            return;

        if (!TryComp(gun, out ContinuousBeamGunComponent? gunComp))
            return;

        if (Exists(gunComp.BeamSoundEnt))
            _audio.Stop(gunComp.BeamSoundEnt.Value);

        var ev = new ContinuousBeamStoppedFiringEvent();
        RaiseLocalEvent(gun, ref ev);
    }
}
