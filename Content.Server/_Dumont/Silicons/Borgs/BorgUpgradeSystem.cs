// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Shared._Dumont.Silicons.Borgs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Wires;

namespace Content.Server._Dumont.Silicons.Borgs;

/// <summary>
/// instala aprimoramentos de borg. a robótica tinha módulos, que são ferramentas, mas nada
/// que deixasse o chassi melhor ao longo do round
/// as assinaturas ficam no item e não no chassi, o BorgSystem já é dono do AfterInteractUsing
/// do chassi e o Robust recusa um segundo inscrito no mesmo par.
/// </summary>
public sealed class BorgUpgradeSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly BorgUpgradeSharedSystem _upgradeShared = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgUpgradeComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<BorgUpgradeComponent, BorgUpgradeDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<BorgUpgradedComponent, ExaminedEvent>(OnExamineBorg);
    }

    private void OnAfterInteract(Entity<BorgUpgradeComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Handled || args.Target is not { } target)
            return;

        if (!HasComp<BorgChassisComponent>(target))
            return;

        if (TryComp<WiresPanelComponent>(target, out var panel) && !panel.Open)
        {
            _popup.PopupEntity(Loc.GetString("borg-panel-not-open"), target, args.User);
            args.Handled = true;
            return;
        }

        if (TryComp<BorgUpgradedComponent>(target, out var upgraded) &&
            upgraded.Installed.Contains(ent.Comp.Name))
        {
            _popup.PopupEntity(Loc.GetString("borg-upgrade-already-installed"), target, args.User);
            args.Handled = true;
            return;
        }

        args.Handled = true;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.InstallTime,
            new BorgUpgradeDoAfterEvent(),
            eventTarget: ent.Owner,
            target: target,
            used: ent.Owner)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    private void OnDoAfter(Entity<BorgUpgradeComponent> ent, ref BorgUpgradeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        if (!TryComp<BorgChassisComponent>(target, out var chassis))
            return;

        args.Handled = true;

        if (TryComp<WiresPanelComponent>(target, out var panel) && !panel.Open)
        {
            _popup.PopupEntity(Loc.GetString("borg-panel-not-open"), target, args.User);
            return;
        }

        var upgraded = EnsureComp<BorgUpgradedComponent>(target);
        if (upgraded.Installed.Contains(ent.Comp.Name))
        {
            _popup.PopupEntity(Loc.GetString("borg-upgrade-already-installed"), target, args.User);
            return;
        }

        EntityManager.AddComponents(target, ent.Comp.Components, removeExisting: false);

        _upgradeShared.GrantModuleSlots((target, chassis), ent.Comp.ExtraModules);

        upgraded.Installed.Add(ent.Comp.Name);

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(args.User):player} installed upgrade {ToPrettyString(ent.Owner)} into borg {ToPrettyString(target)}");

        _popup.PopupEntity(Loc.GetString("borg-upgrade-installed", ("name", Loc.GetString(ent.Comp.Name))),
            target, args.User);

        QueueDel(ent.Owner);
    }

    private void OnExamineBorg(Entity<BorgUpgradedComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Installed.Count == 0)
            return;

        var names = string.Join(", ", ent.Comp.Installed.Select(id => Loc.GetString(id)));
        args.PushMarkup(Loc.GetString("borg-upgrade-examine", ("upgrades", names)));
    }
}
