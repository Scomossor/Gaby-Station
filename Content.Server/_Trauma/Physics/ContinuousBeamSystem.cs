// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Physics;
using Content.Server.Body.Systems;
using Content.Shared.Damage;
using Content.Trauma.Shared.Physics.ComplexJoint;
using Robust.Server.GameStates;
using Robust.Shared.Map;

namespace Content.Trauma.Server.Physics;

public sealed class ContinuousBeamSystem : SharedContinuousBeamSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly PvsOverrideSystem _pvs = default!;

    private readonly HashSet<Entity<DamageableComponent>> _targets = new();

    protected override void OnEndpointShutdown(Entity<ContinuousBeamEndpointComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.PvsOverride)
        {
            _pvs.RemoveGlobalOverride(ent);

            if (ent.Comp.Gun is { } gun)
                _pvs.RemoveGlobalOverride(gun);
        }

        base.OnEndpointShutdown(ent, ref args);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;

        var query = EntityQueryEnumerator<ContinuousBeamGunComponent, ComplexJointVisualsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var gun, out var joint, out var xform))
        {
            if (gun.BeamTime > 0f && now > gun.BeamTimer)
            {
                StopFiring(uid, gun, joint);
                continue;
            }

            if (!UpdateBeamPosition(uid, gun, joint, xform, now))
                continue;

            var ev = new BeforeContinuousBeamDamageTickEvent((uid, gun, joint));
            RaiseLocalEvent(uid, ref ev);
            if (ev.Cancelled)
                continue;

            UpdateBeamDamage(uid, gun, joint, xform, now);
        }
    }

    private bool UpdateBeamPosition(EntityUid uid,
        ContinuousBeamGunComponent gun,
        ComplexJointVisualsComponent joint,
        TransformComponent xform,
        TimeSpan now)
    {
        if (now < gun.UpdateTimer)
            return true;

        gun.UpdateTimer = now + gun.UpdateTime;

        if (!ResolveBeamEndpointData(uid, gun, joint))
            return false;

        var target = gun.ShootCoordinates!.Value;
        var endpoint = gun.Endpoint!.Value;
        var endpointXform = Transform(endpoint);
        var pos = Xform.WithEntityId(endpointXform.Coordinates, target.EntityId);
        var dir = target.Position - pos.Position;
        var len = dir.Length();

        var ourPos = Xform.WithEntityId(xform.Coordinates, target.EntityId).Position;
        var newPos = pos.Position + dir * gun.LaserSpeed / len;
        var dir2 = newPos - ourPos;
        var len2 = dir2.Length();

        if (len2 < 0.01f)
            return true;

        if (len <= gun.LaserSpeed)
        {
            Xform.SetCoordinates(endpoint, endpointXform, target);
            return true;
        }

        var maxRange = MathF.Min(gun.MaxRangeOverride ?? gun.MinMaxLaserRange.Y, gun.MinMaxLaserRange.Y);
        var minRange = MathF.Min(gun.MinMaxLaserRange.X, maxRange);
        var newLen = Math.Clamp(len2, minRange, maxRange);

        Xform.SetCoordinates(endpoint,
            endpointXform,
            new EntityCoordinates(target.EntityId, ourPos + dir2 * newLen / len2));

        return true;
    }

    private void UpdateBeamDamage(EntityUid uid,
        ContinuousBeamGunComponent gun,
        ComplexJointVisualsComponent joint,
        TransformComponent xform,
        TimeSpan now)
    {
        if (now < gun.DamageTimer)
            return;

        gun.DamageTimer = now + gun.DamageTime;

        if (CalculateBeamDamageData((uid, gun, joint)) is not { } tuple)
            return;

        _targets.Clear();
        _lookup.GetEntitiesIntersecting(xform.MapID, tuple.boxRot, _targets, LookupFlags.Uncontained);
        foreach (var target in _targets)
        {
            if (target.Owner == gun.Shooter)
                continue;

            var beforeEv = new BeforeContinuousBeamDamagedEvent(uid, target);
            RaiseLocalEvent(uid, ref beforeEv);

            if (beforeEv.Cancelled)
                continue;

            var damage = gun.Damage;
            if (gun.LimbDamageCompensation)
                damage *= _body.GetVitalBodyPartRatio(target.Owner);

            _damageable.TryChangeDamage(target.Owner,
                damage,
                origin: uid,
                targetPart: gun.TargetPart,
                splitDamage: gun.SplitDamageBehavior);

            if (TerminatingOrDeleted(target.Owner))
                continue;

            var afterEv = new AfterContinuousBeamDamagedEvent(uid, target);
            RaiseLocalEvent(uid, ref afterEv);
        }
    }
}
