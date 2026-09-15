// SPDX-FileCopyrightText: 2026 Project Dumont
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Examine;
using Content.Goobstation.Shared.Xenobiology.Components;
using Content.Shared.Eye;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameStates;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Goobstation.Client.Xenobiology;

public sealed partial class XenobiologyConsoleControlSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    private XenobiologyConsoleStatusControl? _status;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenobiologyConsoleViewComponent, ComponentStartup>(OnViewStartup);
        SubscribeLocalEvent<XenobiologyConsoleViewComponent, ComponentShutdown>(OnViewShutdown);
        SubscribeLocalEvent<XenobiologyConsoleViewComponent, AfterAutoHandleStateEvent>(OnViewState);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);

        _ui.OnScreenChanged += OnScreenChanged;

        CommandBinds.Builder
            .BindBefore(
                ContentKeyFunctions.ExamineEntity,
                new PointerInputCmdHandler(HandleShiftClick, outsidePrediction: true),
                typeof(ExamineSystem))
            .BindBefore(
                ContentKeyFunctions.TryPullObject,
                new PointerInputCmdHandler(HandleControlClick, outsidePrediction: true),
                typeof(SharedInteractionSystem))
            .Register<XenobiologyConsoleControlSystem>();
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<XenobiologyConsoleControlSystem>();
        _ui.OnScreenChanged -= OnScreenChanged;
        RemoveStatus();
        base.Shutdown();
    }

    private void OnViewStartup(Entity<XenobiologyConsoleViewComponent> ent, ref ComponentStartup args)
    {
        if (_player.LocalEntity == ent.Owner)
            AddStatus(ent.Comp);
    }

    private void OnViewShutdown(Entity<XenobiologyConsoleViewComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity == ent.Owner)
            RemoveStatus();
    }

    private void OnViewState(Entity<XenobiologyConsoleViewComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_player.LocalEntity == ent.Owner)
            UpdateStatus(ent.Comp);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        if (TryComp<XenobiologyConsoleViewComponent>(args.Entity, out var view))
            AddStatus(view);
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        RemoveStatus();
    }

    private void OnScreenChanged((UIScreen? Old, UIScreen? New) args)
    {
        RemoveStatus();

        if (args.New != null &&
            _player.LocalEntity is { } player &&
            TryComp<XenobiologyConsoleViewComponent>(player, out var view))
        {
            AddStatus(view);
        }
    }

    private bool HandleShiftClick(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        return SendShortcut(args, XenobiologyConsoleShortcut.ShiftClick);
    }

    private bool HandleControlClick(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        return SendShortcut(args, XenobiologyConsoleShortcut.ControlClick);
    }

    private bool SendShortcut(
        in PointerInputCmdHandler.PointerInputCmdArgs args,
        XenobiologyConsoleShortcut shortcut)
    {
        if (_player.LocalEntity is not { } player ||
            !HasComp<XenobiologyConsoleViewComponent>(player))
        {
            return false;
        }

        NetEntity? target = null;
        var clicked = args.EntityUid;
        var isEyeTarget = TryComp<EyeComponent>(player, out var eye) && eye.Target == clicked;
        if (clicked.IsValid() &&
            Exists(clicked) &&
            clicked != args.Coordinates.EntityId &&
            !isEyeTarget)
        {
            target = GetNetEntity(clicked);
        }

        RaiseNetworkEvent(new XenobiologyConsoleShortcutRequest(
            shortcut,
            target,
            GetNetCoordinates(args.Coordinates)));
        return true;
    }

    private void AddStatus(XenobiologyConsoleViewComponent view)
    {
        if (_status == null)
        {
            if (_ui.ActiveScreen is not { } screen)
                return;

            _status = new XenobiologyConsoleStatusControl();
            screen.AddChild(_status);
            LayoutContainer.SetAnchorAndMarginPreset(
                _status,
                LayoutContainer.LayoutPreset.CenterLeft,
                margin: 12);
        }

        UpdateStatus(view);
    }

    private void UpdateStatus(XenobiologyConsoleViewComponent view)
    {
        _status?.UpdateState(view.StoredSlimes, view.MaxStoredSlimes, view.MonkeyBiomass);
    }

    private void RemoveStatus()
    {
        _status?.Orphan();
        _status = null;
    }
}
