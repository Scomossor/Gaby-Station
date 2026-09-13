// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.Effects;
using Content.Server.Construction.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._Orion.Power.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Contraband;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Wall;
using Robust.Shared.Utility;

namespace Content.Server._Orion.Power.Systems;

public sealed class InducerSystem : EntitySystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SparksSystem _sparks = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InducerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<InducerComponent, InducerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<InducerComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<InducerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
        SubscribeLocalEvent<InducerComponent, GetVerbsEvent<Verb>>(OnGetRmbVerbs);
    }

    private void OnAfterInteract(EntityUid uid, InducerComponent component, AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        var target = args.Target.Value;

        if (!TryComp<BatteryComponent>(target, out var targetBattery))
        {
            _popup.PopupEntity(Loc.GetString("inducer-no-battery"), uid, args.User);
            return;
        }

        if (!CanTransferTo(component, target))
        {
            _popup.PopupEntity(Loc.GetString("inducer-target-contraband"), uid, args.User);
            return;
        }

        if (!_itemSlots.TryGetSlot(uid, component.PowerCellSlotId, out var slot) || slot.Item == null || !TryComp<BatteryComponent>(slot.Item.Value, out var sourceBattery))
        {
            _popup.PopupEntity(Loc.GetString("inducer-no-power-cell"), uid, args.User);
            return;
        }

        if (sourceBattery.CurrentCharge <= 0)
        {
            _popup.PopupEntity(Loc.GetString("inducer-empty"), uid, args.User);
            return;
        }

        if (_battery.IsFull(target, targetBattery))
        {
            _popup.PopupEntity(Loc.GetString("inducer-target-full"), uid, args.User);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, component.TransferDelay, new InducerDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            RequireCanInteract = true,
            DistanceThreshold = component.MaxDistance,
            CancelDuplicate = false,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, InducerComponent component, InducerDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        var target = args.Target.Value;

        if (!TryComp<BatteryComponent>(target, out var targetBattery))
            return;

        if (!CanTransferTo(component, target))
            return;

        if (!_itemSlots.TryGetSlot(uid, component.PowerCellSlotId, out var slot) || slot.Item == null)
            return;

        if (!TryComp<BatteryComponent>(slot.Item.Value, out var sourceBattery))
            return;

        var effectiveMultiplier = UsesStructureMultiplier(target)
            ? component.StructureTransferMultiplier
            : component.TransferMultiplier;

        if (effectiveMultiplier <= 0f)
            return;

        var baseEnergyToConsume = component.TransferRate * component.TransferDelay;
        baseEnergyToConsume = Math.Min(baseEnergyToConsume, sourceBattery.CurrentCharge);

        if (baseEnergyToConsume <= 0)
            return;

        var energyToReceive = baseEnergyToConsume * effectiveMultiplier;
        var freeSpace = targetBattery.MaxCharge - targetBattery.CurrentCharge;
        energyToReceive = Math.Min(energyToReceive, freeSpace);

        var actualEnergyToConsume = energyToReceive / effectiveMultiplier;
        if (_battery.TryUseCharge(slot.Item.Value, actualEnergyToConsume, sourceBattery))
        {
            _battery.AddCharge(target, energyToReceive, targetBattery);
            _sparks.DoSparks(Transform(target).Coordinates);

            args.Repeat = targetBattery.CurrentCharge < targetBattery.MaxCharge;
        }
        else
        {
            _battery.SetCharge(target, targetBattery.CurrentCharge + energyToReceive, targetBattery);
            _battery.SetCharge(slot.Item.Value, sourceBattery.CurrentCharge - actualEnergyToConsume, sourceBattery);
            args.Repeat = false;
        }
    }

    private void OnUseInHand(EntityUid uid, InducerComponent comp, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        CycleMode(uid, comp, args.User);
        args.Handled = true;
    }

    private void OnGetAltVerbs(EntityUid uid, InducerComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (_itemSlots.TryGetSlot(uid, comp.PowerCellSlotId, out var slot) && slot.Item != null)
            return;

    }


    private void OnGetRmbVerbs(EntityUid uid, InducerComponent comp, GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var list = comp.AvailableTransferRates;
        if (list is null || list.Count == 0)
            return;

        var prio = 0;
        foreach (var rate in list)
        {
            var r = rate;
            args.Verbs.Add(new Verb
            {
                Category = VerbCategory.SelectType,
                Text = Loc.GetString("inducer-set-transfer-rate", ("rate", r)),
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/zap.svg.192dpi.png")),
                Priority = prio--,
                Act = () =>
                {
                    if (comp.TransferRate == r) return;
                    comp.TransferRate = r;
                    Dirty(uid, comp);
                    _popup.PopupEntity(Loc.GetString("inducer-transfer-rate-set", ("rate", r)), uid, args.User);
                }
            });
        }
    }


    private void CycleMode(EntityUid uid, InducerComponent comp, EntityUid? user)
    {
        var list = comp.AvailableTransferRates;
        if (list is null || list.Count == 0)
            return;

        var idx = list.IndexOf(comp.TransferRate);

        comp.TransferRate = list[(idx + 1) % list.Count];
        Dirty(uid, comp);

        if (user != null)
            _popup.PopupEntity(Loc.GetString("inducer-transfer-rate-set", ("rate", comp.TransferRate)), uid, user.Value);
    }


    private bool UsesStructureMultiplier(EntityUid target)
    {
        if (Transform(target).Anchored)
            return true;

        return HasComp<ApcComponent>(target)
               || HasComp<MachineComponent>(target)
               || HasComp<WallMountComponent>(target);
    }

    private bool CanTransferTo(InducerComponent component, EntityUid target)
    {
        return component.CombatInducer
               || !TryComp<ContrabandComponent>(target, out var contraband)
             || !contraband.AllowedDepartments.Any(department => department == "Security");
    }
}
