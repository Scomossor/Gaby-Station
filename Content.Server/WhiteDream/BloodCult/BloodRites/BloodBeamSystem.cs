// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Goobstation.Maths.FixedPoint;
using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
using Content.Server.Weapons.Ranged.Systems;
using Content.Server.WhiteDream.BloodCult.Commune;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Wall;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Content.Shared.WhiteDream.BloodCult.Spells;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.WhiteDream.BloodCult.BloodRites;

public sealed partial class BloodBeamSystem : EntitySystem
{
    private static readonly ProtoId<TagPrototype> WallTag = "Wall"; // Dumont

    private readonly HashSet<EntityUid> _queuedStructureConversions = new();

    [Dependency] private readonly TagSystem _tag = default!; // Dumont

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private ITileDefinitionManager _tiles = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private BloodCultCommuneSystem _commune = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private DoAfterSystem _doAfter = default!;
    [Dependency] private GunSystem _gun = default!;
    [Dependency] private MapSystem _map = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private TileSystem _tile = default!;
    [Dependency] private TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodBeamComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<BloodBeamComponent, BloodBeamChargeDoAfterEvent>(OnChargeFinished);
        SubscribeLocalEvent<BloodRiteChantComponent, GunShotEvent>(OnBarrageShot);
        SubscribeLocalEvent<BloodBeamProjectileComponent, PreventCollideEvent>(OnProjectileCollide);
        SubscribeLocalEvent<BloodBeamProjectileComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnAfterInteract(Entity<BloodBeamComponent> beam, ref AfterInteractEvent args)
    {
        if (args.Handled || beam.Comp.Charging || beam.Comp.Firing)
            return;

        if (!HasComp<BloodCultistComponent>(args.User))
        {
            QueueDel(beam);
            return;
        }

        var userPosition = _transform.GetMapCoordinates(args.User).Position;
        var targetPosition = _transform.ToMapCoordinates(args.ClickLocation).Position;
        if ((targetPosition - userPosition).LengthSquared() < 0.01f)
            return;

        var ev = new BloodBeamChargeDoAfterEvent
        {
            AimCoordinates = GetNetCoordinates(args.ClickLocation)
        };
        var doAfter = new DoAfterArgs(EntityManager, args.User, beam.Comp.ChargeTime, ev,
            beam.Owner, beam.Owner, beam.Owner)
        {
            BreakOnMove = false,
            BreakOnDamage = true,
            BreakOnDropItem = true,
            NeedHand = true
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        beam.Comp.Charging = true;
        beam.Comp.User = args.User;
        SpeakEmphaticChant(args.User, beam.Comp.ChargeChantWords); // Dumont
        beam.Comp.NextChargeChant = _timing.CurTime + beam.Comp.ChargeChantInterval;
        _audio.PlayPvs(beam.Comp.ChargeSound, args.User);
        args.Handled = true;
    }

    private void OnBarrageShot(Entity<BloodRiteChantComponent> barrage, ref GunShotEvent args)
    {
        if (barrage.Comp.Spoken)
            return;

        barrage.Comp.Spoken = true;
        Speak(args.User, Loc.GetString(barrage.Comp.Incantation));
    }

    private void OnChargeFinished(Entity<BloodBeamComponent> beam, ref BloodBeamChargeDoAfterEvent args)
    {
        beam.Comp.Charging = false;
        if (args.Handled || args.Cancelled || !HasComp<BloodCultistComponent>(args.User) ||
            !_hands.IsHolding(args.User, beam))
            return;

        var userPosition = _transform.GetMapCoordinates(args.User).Position;
        var targetPosition = _transform.ToMapCoordinates(GetCoordinates(args.AimCoordinates)).Position;
        var direction = targetPosition - userPosition;
        if (direction.LengthSquared() < 0.01f)
            return;

        args.Handled = true;
        beam.Comp.User = args.User;
        beam.Comp.AimAngle = direction.ToWorldAngle();
        beam.Comp.ShotsFired = 0;
        beam.Comp.NextShot = _timing.CurTime;
        beam.Comp.Firing = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var beams = EntityQueryEnumerator<BloodBeamComponent>();
        while (beams.MoveNext(out var uid, out var beam))
        {
            if (beam.Charging && _timing.CurTime >= beam.NextChargeChant &&
                beam.User is { } chargingUser && !TerminatingOrDeleted(chargingUser) &&
                _hands.IsHolding(chargingUser, uid))
            {
                SpeakEmphaticChant(chargingUser, beam.ChargeChantWords); // Dumont
                beam.NextChargeChant += beam.ChargeChantInterval;
            }

            if (!beam.Firing || _timing.CurTime < beam.NextShot)
                continue;

            if (beam.User is not { } user || TerminatingOrDeleted(user) || !_hands.IsHolding(user, uid))
            {
                QueueDel(uid);
                continue;
            }

            FireShot((uid, beam), user);
            beam.ShotsFired++;

            if (beam.ShotsFired >= beam.ShotCount)
            {
                _stun.TryAddParalyzeDuration(user, TimeSpan.FromSeconds(4));
                QueueDel(uid);
                continue;
            }

            beam.NextShot += beam.ShotInterval;
        }

        var projectiles = EntityQueryEnumerator<BloodBeamProjectileComponent, TransformComponent>();
        while (projectiles.MoveNext(out var uid, out var projectile, out var xform))
            CorruptFloor((uid, projectile, xform));
    }

    private void FireShot(Entity<BloodBeamComponent> beam, EntityUid user)
    {
        var pair = beam.Comp.ShotsFired / 2;
        var spread = MathF.Max(0f, beam.Comp.InitialSpread - pair * beam.Comp.SpreadStep);
        var signedSpread = beam.Comp.ShotsFired % 2 == 0 ? spread : -spread;
        var direction = (beam.Comp.AimAngle + Angle.FromDegrees(signedSpread)).ToWorldVec();
        var origin = _transform.GetMapCoordinates(user);
        var projectile = Spawn(beam.Comp.ProjectilePrototype, origin);

        _gun.ShootProjectile(projectile, direction, Vector2.Zero, beam, user, beam.Comp.ProjectileSpeed);
        var chant = _commune.GenerateChant(beam.Comp.FireChantWords).TrimEnd('!', ' ');
        Speak(user, $"{chant}!!!");
        _audio.PlayPvs(beam.Comp.FireSound, user);
    }

    private void Speak(EntityUid user, string message)
    {
        _chat.TrySendInGameICMessage(user,
            message,
            InGameICChatType.Speak,
            ChatTransmitRange.Normal);
    }

    private void SpeakEmphaticChant(EntityUid user, int words)
    {
        var chant = _commune.GenerateChant(words).TrimEnd('!', ' ');
        Speak(user, $"{chant}!!!");
    }

    private void OnProjectileCollide(Entity<BloodBeamProjectileComponent> projectile, ref PreventCollideEvent args)
    {
        if (_queuedStructureConversions.Contains(args.OtherEntity) ||
            IsCultStructure(args.OtherEntity, projectile.Comp))
        {
            args.Cancelled = true;
            return;
        }

        if (projectile.Comp.HitEntities.Contains(args.OtherEntity))
        {
            args.Cancelled = true;
            return;
        }

        if (!HasComp<BloodCultistComponent>(args.OtherEntity))
            return;

        projectile.Comp.HitEntities.Add(args.OtherEntity);
        HealCultist(args.OtherEntity, projectile.Comp.CultistHealing);
        args.Cancelled = true;
    }

    private void OnProjectileHit(Entity<BloodBeamProjectileComponent> projectile, ref ProjectileHitEvent args)
    {
        if (!projectile.Comp.HitEntities.Add(args.Target))
            return;

        if (TryConvertStructure(projectile.Comp, args.Target) || HasComp<BloodCultistComponent>(args.Target))
            return;

        _stun.TryAddParalyzeDuration(args.Target, projectile.Comp.ParalyzeTime);
    }

    private bool TryConvertStructure(BloodBeamProjectileComponent projectile, EntityUid target)
    {
        if (IsCultStructure(target, projectile))
            return true;

        EntProtoId? replacement = null;

        if (HasComp<AirlockComponent>(target))
            replacement = projectile.CultDoor;
        else if (_tag.HasTag(target, WallTag)) // Dumont
            replacement = projectile.CultWall;

        if (replacement is not { } prototype)
            return false;

        if (!_queuedStructureConversions.Add(target))
            return true;

        var xform = Transform(target);
        Spawn(prototype,
            _transform.GetMapCoordinates(target, xform),
            rotation: _transform.GetWorldRotation(xform));
        Spawn(projectile.TileEffect, xform.Coordinates);
        QueueDel(target);
        return true;
    }

    private bool IsCultStructure(EntityUid target, BloodBeamProjectileComponent projectile)
    {
        var prototype = MetaData(target).EntityPrototype?.ID;
        return prototype == projectile.CultWall.Id || prototype == projectile.CultDoor.Id;
    }

    private void HealCultist(EntityUid target, float amount)
    {
        if (!TryComp(target, out DamageableComponent? damageable))
            return;

        var healingLeft = amount;
        foreach (var (type, value) in damageable.Damage.DamageDict) // Dumont
        {
            if (value <= 0 || !_prototypes.TryIndex(type, out DamageTypePrototype? damageType))
                continue;

            var healed = value > FixedPoint2.New(healingLeft) ? FixedPoint2.New(healingLeft) : value;
            _damageable.TryChangeDamage(target, new DamageSpecifier(damageType, -healed), origin: target);
            healingLeft -= (float) healed;
            if (healingLeft <= 0)
                break;
        }
    }

    private void CorruptFloor(Entity<BloodBeamProjectileComponent, TransformComponent> projectile)
    {
        if (projectile.Comp2.GridUid is not { } gridUid ||
            !TryComp(gridUid, out MapGridComponent? grid) ||
            !_map.TryGetTileRef(gridUid, grid, projectile.Comp2.Coordinates, out var tileRef) ||
            projectile.Comp1.LastTile == tileRef.GridIndices)
            return;

        projectile.Comp1.LastTile = tileRef.GridIndices;

        // Check the tile itself as well as collision hits. Open airlocks have no blocking fixture,
        // so relying on projectile collision alone would allow the beam to skip them.
        foreach (var anchored in _map.GetAnchoredEntities(gridUid, grid, tileRef.GridIndices).ToList())
            TryConvertStructure(projectile.Comp1, anchored);

        if (tileRef.Tile.IsEmpty)
            return;

        var cultTile = (ContentTileDefinition) _tiles[projectile.Comp1.CultTile];
        if (tileRef.Tile.TypeId == cultTile.TileId)
            return;

        _tile.ReplaceTile(tileRef, cultTile);
        Spawn(projectile.Comp1.TileEffect, _map.GridTileToLocal(gridUid, grid, tileRef.GridIndices));
    }
}
