// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Blocking;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Ghost;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.WhiteDream.BloodCult.Items;

public sealed partial class CultItemSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    // Dumont changes start
    [Dependency] private INetManager _net = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    // Dumont end

    public override void Initialize()
    {
        SubscribeLocalEvent<CultItemComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<CultItemComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<CultItemComponent, BeforeThrowEvent>(OnBeforeGettingThrown); // Dumont
        SubscribeLocalEvent<CultItemComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<CultItemComponent, AttemptMeleeEvent>(OnMeleeAttempt);
        SubscribeLocalEvent<CultItemComponent, BeforeBlockingEvent>(OnBeforeBlocking);
        // Dumont
        SubscribeLocalEvent<CultItemComponent, ActivatableUIOpenAttemptEvent>(OnUiOpenAttempt);
    }

    private void OnActivate(Entity<CultItemComponent> item, ref ActivateInWorldEvent args)
    {
        if (CanUse(args.User))
            return;

        args.Handled = true;
        KnockdownAndDropItem(item, args.User, Loc.GetString("cult-item-component-generic"));
    }

    private void OnUseInHand(Entity<CultItemComponent> item, ref UseInHandEvent args)
    {
        if (CanUse(args.User) ||
            // Allow non-cultists to remove embedded cultist weapons and getting knocked down afterwards on pickup
            (TryComp<EmbeddableProjectileComponent>(item.Owner, out var embeddable) && embeddable.EmbeddedIntoUid != null))
            return;

        args.Handled = true;
        KnockdownAndDropItem(item, args.User, Loc.GetString("cult-item-component-generic"));
    }

    private void OnBeforeGettingThrown(Entity<CultItemComponent> item, ref BeforeThrowEvent args) // Dumont
    {
        if (CanUse(args.PlayerUid))
            return;

        args.Cancelled = true;
        KnockdownAndDropItem(item, args.PlayerUid, Loc.GetString("cult-item-component-throw-fail"), true);
    }

    private void OnEquipAttempt(Entity<CultItemComponent> item, ref BeingEquippedAttemptEvent args)
    {
        if (CanUse(args.EquipTarget))
            return;

        args.Cancel();
        KnockdownAndDropItem(item, args.Equipee, Loc.GetString("cult-item-component-equip-fail")); // Dumont
    }

    private void OnMeleeAttempt(Entity<CultItemComponent> item, ref AttemptMeleeEvent args)
    {
        if (CanUse(args.User))
            return;

        args.Cancelled = true;
        KnockdownAndDropItem(item, args.User, Loc.GetString("cult-item-component-attack-fail"));
    }

    private void OnBeforeBlocking(Entity<CultItemComponent> item, ref BeforeBlockingEvent args)
    {
        if (CanUse(args.User))
            return;

        args.Cancel();
        KnockdownAndDropItem(item, args.User, Loc.GetString("cult-item-component-block-fail"));
    }

    // Dumont
    private void OnUiOpenAttempt(Entity<CultItemComponent> item, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled || CanUse(args.User))
            return;

        args.Cancel();
        KnockdownAndDropItem(item, args.User, Loc.GetString("cult-item-component-generic"));
    }

    // serverOnly is a very rough hack to make sure OnBeforeGettingThrown (that is only run server-side) can
    // show the popup while not causing several popups to show up with PopupEntity.
    private void KnockdownAndDropItem(Entity<CultItemComponent> item, EntityUid user, string message, bool serverOnly = false)
    {
        if (serverOnly)
            _popup.PopupEntity(message, item, user);
        else
            _popup.PopupPredicted(message, item, user);
        _stun.TryKnockdown(user, item.Comp.KnockdownDuration, true);
        _stun.TryAddStunDuration(user, item.Comp.StunDuration); // Dumont
        _hands.TryDrop(user);

        Backlash(item, user); // Dumont
    }

    /// <summary>
    ///     Shoves the user away and cuts them for holding a cult item.
    /// </summary>
    private void Backlash(Entity<CultItemComponent> item, EntityUid user)
    {
        var direction = -_transform.GetWorldRotation(user).ToWorldVec();
        _physics.ApplyLinearImpulse(user, direction * item.Comp.BacklashForce);

        if (_net.IsServer)
            _damageable.TryChangeDamage(user, item.Comp.BacklashDamage, true, origin: item.Owner);
    }

    private bool CanUse(EntityUid? uid) => HasComp<BloodCultistComponent>(uid) || HasComp<GhostComponent>(uid);
}
