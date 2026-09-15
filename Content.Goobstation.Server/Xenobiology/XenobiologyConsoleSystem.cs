// SPDX-FileCopyrightText: 2026 Project Dumont
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.SurveillanceCamera;
using Content.Goobstation.Shared.Xenobiology;
using Content.Goobstation.Shared.Xenobiology.Components;
using Content.Goobstation.Server.Xenobiology.SlimeGrinder;
using Content.Server.Chat.Managers;
using Content.Server.DeviceLinking.Systems;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Power.Components;
using Content.Server.SurveillanceCamera;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.Eye;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Power;
using Content.Shared.Popups;
using Content.Shared.Pulling.Events;
using Content.Shared.Tag;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Goobstation.Server.Xenobiology;

public sealed partial class XenobiologyConsoleSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private DeviceLinkSystem _deviceLink = default!;
    [Dependency] private SharedEyeSystem _eye = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private HTNSystem _htn = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private NPCSystem _npc = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SlimeScannerSystem _slimeScanner = default!;
    [Dependency] private SlimeGrinderSystem _slimeGrinder = default!;
    [Dependency] private SurveillanceCameraSystem _surveillanceCamera = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedConsoleCameraSystem _cameraVision = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenobiologyConsoleComponent, ComponentStartup>(OnConsoleStartup);
        SubscribeLocalEvent<XenobiologyConsoleComponent, ComponentShutdown>(OnConsoleShutdown);
        SubscribeLocalEvent<XenobiologyConsoleComponent, ActivateInWorldEvent>(OnConsoleActivated);
        SubscribeLocalEvent<XenobiologyConsoleComponent, AfterInteractUsingEvent>(OnConsoleAfterInteractUsing);
        SubscribeLocalEvent<XenobiologyConsoleComponent, PowerChangedEvent>(OnConsolePowerChanged);
        SubscribeLocalEvent<XenobiologyConsoleComponent, EntRemovedFromContainerMessage>(OnConsoleContainerRemoved);
        SubscribeLocalEvent<XenobiologyConsoleControllerComponent, ComponentShutdown>(OnControllerShutdown);
        SubscribeLocalEvent<XenobiologyConsoleControllerComponent, MoveEvent>(OnControllerMoved);
        SubscribeLocalEvent<XenobiologyConsoleControllerComponent, EntParentChangedMessage>(OnControllerParentChanged);
        SubscribeLocalEvent<XenobiologyConsoleControllerComponent, PullStartedMessage>(OnControllerPullStarted);
        SubscribeLocalEvent<XenobiologyConsoleControllerComponent, BeingPulledAttemptEvent>(OnControllerBeingPulledAttempt);
        SubscribeLocalEvent<XenobiologyConsoleRemoteComponent, MoveEvent>(OnRemoteMoved);

        SubscribeLocalEvent<XenobiologyConsoleStoredSlimeComponent, UpdateCanMoveEvent>(OnStoredSlimeMoveAttempt);
        SubscribeLocalEvent<XenobiologyConsoleStoredSlimeComponent, AttackAttemptEvent>(OnStoredSlimeAttackAttempt);
        SubscribeLocalEvent<XenobiologyConsoleStoredSlimeComponent, UseAttemptEvent>(OnStoredSlimeUseAttempt);
        SubscribeLocalEvent<XenobiologyConsoleStoredSlimeComponent, StartPullAttemptEvent>(OnStoredSlimePullAttempt);
        SubscribeLocalEvent<XenobiologyConsoleStoredSlimeComponent, ComponentShutdown>(OnStoredSlimeShutdown);

        SubscribeLocalEvent<XenobiologyConsoleExitEvent>(OnExit);
        SubscribeLocalEvent<XenobiologyConsolePlaceMonkeyEvent>(OnPlaceMonkey);
        SubscribeLocalEvent<XenobiologyConsoleRecycleMonkeyEvent>(OnRecycleMonkey);
        SubscribeLocalEvent<XenobiologyConsoleGrabSlimeEvent>(OnGrabSlime);
        SubscribeLocalEvent<XenobiologyConsoleReleaseSlimesEvent>(OnReleaseSlimes);
        SubscribeLocalEvent<XenobiologyConsoleAnalyzeSlimeEvent>(OnAnalyzeSlime);
        SubscribeLocalEvent<XenobiologyConsoleShowShortcutsEvent>(OnShowShortcuts);

        SubscribeNetworkEvent<XenobiologyConsoleShortcutRequest>(OnShortcutRequest);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<XenobiologyConsoleControllerComponent>();
        while (query.MoveNext(out var uid, out var controller))
        {
            if (_gameTiming.CurTime < controller.NextValidationTime)
                continue;

            controller.NextValidationTime = _gameTiming.CurTime + GetValidationInterval(controller);

            if (!IsSessionValid(uid, controller))
            {
                StopControl((uid, controller), removeController: true);
                continue;
            }

            ValidateCameraCoverage((uid, controller));
        }
    }

    private void OnConsoleStartup(Entity<XenobiologyConsoleComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.SlimeContainer = _container.EnsureContainer<Container>(ent, XenobiologyConsoleComponent.SlimeContainerId);
    }

    private void OnConsoleShutdown(Entity<XenobiologyConsoleComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.ActiveUser is { } user && TryComp<XenobiologyConsoleControllerComponent>(user, out var controller))
            StopControl((user, controller), removeController: true);

        ReleaseSlimes(ent, Transform(ent).Coordinates);
        QueueDel(ent.Comp.RemoteEntity);
    }

    private void OnConsoleActivated(Entity<XenobiologyConsoleComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (!HasConsoleAccess(args.User, ent))
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-console-no-access"), ent, args.User);
            args.Handled = true;
            return;
        }

        if (!IsConsolePowered(ent))
            return;

        if (ent.Comp.ActiveUser is { } active && active != args.User && !Terminating(active))
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-console-in-use"), ent, args.User);
            args.Handled = true;
            return;
        }

        if (ent.Comp.ActiveUser == args.User && TryComp<XenobiologyConsoleControllerComponent>(args.User, out var activeController))
        {
            StopControl((args.User, activeController), removeController: true);
            args.Handled = true;
            return;
        }

        StartControl(args.User, ent);
        args.Handled = true;
    }

    private void OnConsoleAfterInteractUsing(Entity<XenobiologyConsoleComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach || !_tag.HasTag(args.Used, ent.Comp.MonkeyCubeTag))
            return;

        if (!HasConsoleAccess(args.User, ent))
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-console-no-access"), ent, args.User);
            args.Handled = true;
            return;
        }

        ent.Comp.MonkeyBiomass += ent.Comp.MonkeyCubeBiomass;
        QueueDel(args.Used);
        UpdateViewState(ent);
        _popup.PopupEntity(Loc.GetString("xenobiology-console-cube-inserted", ("amount", ent.Comp.MonkeyBiomass)), ent, args.User);
        args.Handled = true;
    }

    private void OnConsolePowerChanged(Entity<XenobiologyConsoleComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered ||
            ent.Comp.ActiveUser is not { } user ||
            !TryComp<XenobiologyConsoleControllerComponent>(user, out var controller))
        {
            return;
        }

        StopControl((user, controller), removeController: true);
    }

    private void OnConsoleContainerRemoved(Entity<XenobiologyConsoleComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != XenobiologyConsoleComponent.SlimeContainerId)
            return;

        RestoreStoredSlime(args.Entity);
        UpdateViewState(ent);
    }

    private void OnControllerShutdown(Entity<XenobiologyConsoleControllerComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.CleaningUp)
            return;

        StopControl(ent, removeController: false);
    }

    private void OnControllerMoved(Entity<XenobiologyConsoleControllerComponent> ent, ref MoveEvent args)
    {
        StopControlIfSessionInvalid(ent);
    }

    private void OnControllerParentChanged(Entity<XenobiologyConsoleControllerComponent> ent, ref EntParentChangedMessage args)
    {
        StopControlIfSessionInvalid(ent);
    }

    private void OnControllerPullStarted(Entity<XenobiologyConsoleControllerComponent> ent, ref PullStartedMessage args)
    {
        StopControl(ent, removeController: true);
    }

    private void OnControllerBeingPulledAttempt(Entity<XenobiologyConsoleControllerComponent> ent, ref BeingPulledAttemptEvent args)
    {
        StopControl(ent, removeController: true);
    }

    private void OnRemoteMoved(Entity<XenobiologyConsoleRemoteComponent> ent, ref MoveEvent args)
    {
        if (ent.Comp.ReturningToCameraView ||
            TerminatingOrDeleted(ent) ||
            ent.Comp.Controller is not { } user ||
            !TryComp<XenobiologyConsoleControllerComponent>(user, out var controller) ||
            controller.Console is not { } consoleUid ||
            !TryComp<XenobiologyConsoleComponent>(consoleUid, out var console))
        {
            return;
        }

        var controllerEnt = new Entity<XenobiologyConsoleControllerComponent>(user, controller);
        var consoleEnt = new Entity<XenobiologyConsoleComponent>(consoleUid, console);

        if (TryRefreshCameraCoverage(controllerEnt, consoleEnt, args.NewPosition))
            return;

        EntityCoordinates? fallback = null;
        if (controller.LastValidCoordinates is { } lastValid &&
            TryFindVisibleCamera(lastValid, console, out _))
        {
            fallback = lastValid;
        }
        else if (TryFindVisibleCamera(args.OldPosition, console, out _))
        {
            fallback = args.OldPosition;
        }

        if (fallback is not { } visibleFallback)
        {
            StopControl(controllerEnt, removeController: true);
            return;
        }

        ent.Comp.ReturningToCameraView = true;
        try
        {
            _transform.SetCoordinates(ent, visibleFallback);
        }
        finally
        {
            ent.Comp.ReturningToCameraView = false;
        }

        if (!TryRefreshCameraCoverage(controllerEnt, consoleEnt, visibleFallback))
            StopControl(controllerEnt, removeController: true);
    }

    private void StartControl(EntityUid user, Entity<XenobiologyConsoleComponent> console)
    {
        if (TryComp<XenobiologyConsoleControllerComponent>(user, out var oldController))
        {
            StopControl((user, oldController), removeController: true);
            return;
        }

        if (!TryFindInitialCamera(console, out var camera))
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-console-no-cameras"), console, user);
            return;
        }

        if (console.Comp.SlimeContainer is null ||
            console.Comp.SlimeContainer.Owner == EntityUid.Invalid)
            console.Comp.SlimeContainer = _container.EnsureContainer<Container>(console, XenobiologyConsoleComponent.SlimeContainerId);

        var remote = Spawn(console.Comp.RemoteEntityPrototype, Transform(camera).Coordinates);
        var remoteComp = EnsureComp<XenobiologyConsoleRemoteComponent>(remote);
        remoteComp.Controller = user;

        var controller = EnsureComp<XenobiologyConsoleControllerComponent>(user);
        controller.Console = console;
        controller.RemoteEntity = remote;
        controller.ActiveCamera = camera;
        controller.LastValidCoordinates = Transform(remote).Coordinates;
        controller.NextValidationTime = _gameTiming.CurTime + console.Comp.SessionValidationInterval;
        _actions.AddAction(user, ref controller.ExitActionEntity, controller.ExitAction);
        _actions.AddAction(user, ref controller.PlaceMonkeyActionEntity, controller.PlaceMonkeyAction);
        _actions.AddAction(user, ref controller.RecycleMonkeyActionEntity, controller.RecycleMonkeyAction);
        _actions.AddAction(user, ref controller.GrabSlimeActionEntity, controller.GrabSlimeAction);
        _actions.AddAction(user, ref controller.ReleaseSlimesActionEntity, controller.ReleaseSlimesAction);
        _actions.AddAction(user, ref controller.AnalyzeSlimeActionEntity, controller.AnalyzeSlimeAction);
        _actions.AddAction(user, ref controller.ShowShortcutsActionEntity, controller.ShowShortcutsAction);
        SetOwnActionIcon(controller.AnalyzeSlimeActionEntity);

        if (TryComp(user, out EyeComponent? eyeComp))
        {
            controller.PreviousEyeTarget = eyeComp.Target;
            controller.PreviousDrawFov = eyeComp.DrawFov;
            controller.PreviousDrawLight = eyeComp.DrawLight;
            _eye.SetTarget(user, remote, eyeComp);
            _eye.SetDrawFov(user, false, eyeComp);
            _eye.SetDrawLight((user, eyeComp), true);
        }

        var view = EnsureComp<XenobiologyConsoleViewComponent>(user);
        view.RequiredCameraTag = console.Comp.RequiredCameraTag;
        view.CameraOverlaySearchRange = console.Comp.CameraOverlaySearchRange;
        UpdateViewState(console, (user, view));

        AddCameraViewer(camera, user, console);

        if (TryComp<RelayInputMoverComponent>(user, out var relay))
        {
            var relayEntity = relay.RelayEntity;
            if (relayEntity.IsValid() && !TerminatingOrDeleted(relayEntity))
                controller.PreviousRelayEntity = relayEntity;
        }

        _mover.SetRelay(user, remote);

        console.Comp.ActiveUser = user;
        console.Comp.RemoteEntity = remote;

        _popup.PopupEntity(Loc.GetString("xenobiology-console-connected"), console, user);
    }

    private void SetOwnActionIcon(EntityUid? actionEntity)
    {
        if (actionEntity is not { } actionUid ||
            !TryComp<ActionComponent>(actionUid, out var action))
        {
            return;
        }

        _actions.SetEntityIcon((actionUid, action), actionUid);
    }

    private void StopControl(Entity<XenobiologyConsoleControllerComponent> controller, bool removeController)
    {
        if (controller.Comp.CleaningUp)
            return;

        controller.Comp.CleaningUp = true;

        if (controller.Comp.Console is { } consoleUid && TryComp<XenobiologyConsoleComponent>(consoleUid, out var console))
        {
            if (console.ActiveUser == controller)
                console.ActiveUser = null;

            if (console.RemoteEntity == controller.Comp.RemoteEntity)
                console.RemoteEntity = null;

        }

        _actions.RemoveAction(controller.Comp.ExitActionEntity);
        _actions.RemoveAction(controller.Comp.PlaceMonkeyActionEntity);
        _actions.RemoveAction(controller.Comp.RecycleMonkeyActionEntity);
        _actions.RemoveAction(controller.Comp.GrabSlimeActionEntity);
        _actions.RemoveAction(controller.Comp.ReleaseSlimesActionEntity);
        _actions.RemoveAction(controller.Comp.AnalyzeSlimeActionEntity);
        _actions.RemoveAction(controller.Comp.ShowShortcutsActionEntity);

        RemComp<XenobiologyConsoleViewComponent>(controller);

        if (controller.Comp.ActiveCamera is { } camera)
            RemoveCameraViewer(camera, controller, controller.Comp.Console);

        if (TryComp(controller, out EyeComponent? eyeComp))
        {
            var previousTarget = controller.Comp.PreviousEyeTarget;
            if (previousTarget is { } target && TerminatingOrDeleted(target))
                previousTarget = null;

            _eye.SetTarget(controller, previousTarget, eyeComp);
            _eye.SetDrawFov(controller, controller.Comp.PreviousDrawFov ?? true, eyeComp);
            _eye.SetDrawLight((controller, eyeComp), controller.Comp.PreviousDrawLight ?? true);
        }

        if (controller.Comp.PreviousRelayEntity is { } previousRelay &&
            !TerminatingOrDeleted(previousRelay))
        {
            _mover.SetRelay(controller, previousRelay);
        }
        else
        {
            RemComp<RelayInputMoverComponent>(controller);
        }

        QueueDel(controller.Comp.RemoteEntity);

        if (removeController && !Terminating(controller))
            RemCompDeferred<XenobiologyConsoleControllerComponent>(controller);
    }

    private void ValidateCameraCoverage(Entity<XenobiologyConsoleControllerComponent> controller)
    {
        if (controller.Comp.RemoteEntity is not { } remote ||
            controller.Comp.Console is not { } consoleUid ||
            !TryComp<XenobiologyConsoleComponent>(consoleUid, out var console))
        {
            return;
        }

        var remoteCoordinates = Transform(remote).Coordinates;
        if (TryRefreshCameraCoverage(controller, (consoleUid, console), remoteCoordinates))
            return;

        if (controller.Comp.LastValidCoordinates is { } lastValid &&
            TryFindVisibleCamera(lastValid, console, out _) &&
            TryComp<XenobiologyConsoleRemoteComponent>(remote, out var remoteComp))
        {
            remoteComp.ReturningToCameraView = true;
            try
            {
                _transform.SetCoordinates(remote, lastValid);
            }
            finally
            {
                remoteComp.ReturningToCameraView = false;
            }

            if (!TryRefreshCameraCoverage(controller, (consoleUid, console), lastValid))
                StopControl(controller, removeController: true);

            return;
        }

        StopControl(controller, removeController: true);
    }

    private bool TryRefreshCameraCoverage(
        Entity<XenobiologyConsoleControllerComponent> controller,
        Entity<XenobiologyConsoleComponent> console,
        EntityCoordinates coordinates)
    {
        if (!coordinates.IsValid(EntityManager))
            return false;

        if (controller.Comp.ActiveCamera is { } activeCamera &&
            IsVisibleFromCamera(activeCamera, console.Comp, coordinates))
        {
            controller.Comp.LastValidCoordinates = coordinates;
            return true;
        }

        if (!TryFindVisibleCamera(coordinates, console.Comp, out var camera))
            return false;

        controller.Comp.LastValidCoordinates = coordinates;
        SetActiveCamera(controller, console, camera);
        return true;
    }

    private void SetActiveCamera(
        Entity<XenobiologyConsoleControllerComponent> controller,
        Entity<XenobiologyConsoleComponent> console,
        EntityUid camera)
    {
        if (controller.Comp.ActiveCamera == camera)
            return;

        if (controller.Comp.ActiveCamera is { } oldCamera)
            RemoveCameraViewer(oldCamera, controller, console);

        AddCameraViewer(camera, controller, console);
        controller.Comp.ActiveCamera = camera;
    }

    private void AddCameraViewer(EntityUid camera, EntityUid user, EntityUid? monitor)
    {
        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        _surveillanceCamera.AddActiveViewer(camera, user, monitor, actor: actor);
    }

    private void RemoveCameraViewer(EntityUid camera, EntityUid user, EntityUid? monitor)
    {
        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        _surveillanceCamera.RemoveActiveViewer(camera, user, monitor, actor: actor);
    }

    private bool TryFindInitialCamera(Entity<XenobiologyConsoleComponent> console, out EntityUid camera)
    {
        return TryFindVisibleCamera(console, console.Comp, out camera) ||
               TryFindClosestCamera(console, console.Comp, out camera);
    }

    private bool TryFindVisibleCamera(EntityUid target, XenobiologyConsoleComponent console, out EntityUid camera)
    {
        return TryFindVisibleCamera(Transform(target).Coordinates, console, out camera);
    }

    private bool TryFindVisibleCamera(EntityCoordinates targetCoords, XenobiologyConsoleComponent console, out EntityUid camera)
    {
        var closestDistance = float.MaxValue;
        camera = default;

        foreach (var candidate in FindAvailableCameras(targetCoords, console))
        {
            if (!IsVisibleFromCamera(candidate, console, targetCoords))
                continue;

            if (!targetCoords.TryDistance(EntityManager, Transform(candidate).Coordinates, out var distance) ||
                distance >= closestDistance)
            {
                continue;
            }

            closestDistance = distance;
            camera = candidate;
        }

        return closestDistance < float.MaxValue;
    }

    private bool IsVisibleFromCamera(EntityUid camera, XenobiologyConsoleComponent console, EntityCoordinates targetCoords)
    {
        if (!targetCoords.IsValid(EntityManager) ||
            !TryComp<ConsoleCameraComponent>(camera, out var consoleCamera) ||
            !consoleCamera.Tags.Contains(console.RequiredCameraTag) ||
            !TryComp<SurveillanceCameraComponent>(camera, out var surveillanceCamera) ||
            !surveillanceCamera.Active ||
            Transform(camera).MapID != _transform.ToMapCoordinates(targetCoords).MapId)
        {
            return false;
        }

        return IsInCameraView(camera, consoleCamera, targetCoords);
    }

    private bool TryFindClosestCamera(EntityUid target, XenobiologyConsoleComponent console, out EntityUid camera)
    {
        var targetCoords = Transform(target).Coordinates;
        var closestDistance = float.MaxValue;
        camera = default;

        foreach (var candidate in FindAvailableCameras(targetCoords, console))
        {
            if (!targetCoords.TryDistance(EntityManager, Transform(candidate).Coordinates, out var distance) ||
                distance >= closestDistance)
            {
                continue;
            }

            closestDistance = distance;
            camera = candidate;
        }

        return closestDistance < float.MaxValue;
    }

    private IEnumerable<Entity<ConsoleCameraComponent>> FindAvailableCameras(
        EntityCoordinates targetCoords,
        XenobiologyConsoleComponent console)
    {
        var targetMap = _transform.ToMapCoordinates(targetCoords).MapId;
        foreach (var candidate in _lookup.GetEntitiesInRange<ConsoleCameraComponent>(
                     targetCoords,
                     console.CameraSearchRange,
                     LookupFlags.Static | LookupFlags.Dynamic | LookupFlags.Approximate))
        {
            if (!candidate.Comp.Tags.Contains(console.RequiredCameraTag) ||
                !TryComp<SurveillanceCameraComponent>(candidate, out var surveillanceCamera) ||
                !surveillanceCamera.Active ||
                Transform(candidate).MapID != targetMap)
            {
                continue;
            }

            yield return candidate;
        }
    }

    private bool IsInCameraView(EntityUid camera, ConsoleCameraComponent consoleCamera, EntityCoordinates targetCoords)
    {
        return _cameraVision.TryCreateVision(camera, consoleCamera, out var vision) &&
               _cameraVision.IsVisible(vision, targetCoords);
    }

    private void OnExit(XenobiologyConsoleExitEvent ev)
    {
        if (!TryComp<XenobiologyConsoleControllerComponent>(ev.Performer, out var controller))
            return;

        StopControl((ev.Performer, controller), removeController: true);
        ev.Handled = true;
    }

    private void OnPlaceMonkey(XenobiologyConsolePlaceMonkeyEvent ev)
    {
        if (!TryGetSession(ev.Performer, out var console, out var remote))
            return;

        TryPlaceMonkey(ev.Performer, console, Transform(remote).Coordinates);
        ev.Handled = true;
    }

    private void OnRecycleMonkey(XenobiologyConsoleRecycleMonkeyEvent ev)
    {
        if (!TryGetSession(ev.Performer, out var console, out var remote))
            return;

        if (!TryFindMonkey(remote, console.Comp, out var monkey))
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-console-no-monkey"), ev.Performer, ev.Performer);
            ev.Handled = true;
            return;
        }

        TryRecycleMonkey(ev.Performer, console, monkey);
        ev.Handled = true;
    }

    private void OnGrabSlime(XenobiologyConsoleGrabSlimeEvent ev)
    {
        if (!TryGetSession(ev.Performer, out var console, out var remote))
            return;

        if (!TryFindSlime(remote, console.Comp, _ => true, out var slime))
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-console-no-slime"), ev.Performer, ev.Performer);
            ev.Handled = true;
            return;
        }

        TryGrabSlime(ev.Performer, console, slime);
        ev.Handled = true;
    }

    private void OnReleaseSlimes(XenobiologyConsoleReleaseSlimesEvent ev)
    {
        if (!TryGetSession(ev.Performer, out var console, out var remote))
            return;

        TryReleaseSlimes(ev.Performer, console, Transform(remote).Coordinates);
        ev.Handled = true;
    }

    private void OnAnalyzeSlime(XenobiologyConsoleAnalyzeSlimeEvent ev)
    {
        if (!TryGetSession(ev.Performer, out var console, out var remote))
            return;

        if (!TryFindSlime(remote, console.Comp, _ => true, out var slime))
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-console-no-slime"), ev.Performer, ev.Performer);
            ev.Handled = true;
            return;
        }

        AnalyzeSlime(ev.Performer, slime);
        ev.Handled = true;
    }

    private void OnShowShortcuts(XenobiologyConsoleShowShortcutsEvent ev)
    {
        if (!TryGetSession(ev.Performer, out _, out _) ||
            !TryComp<ActorComponent>(ev.Performer, out var actor))
        {
            return;
        }

        _chat.DispatchServerMessage(actor.PlayerSession, Loc.GetString("xenobiology-console-shortcuts-chat"));
        ev.Handled = true;
    }

    private void OnShortcutRequest(XenobiologyConsoleShortcutRequest request, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user ||
            !TryComp<XenobiologyConsoleControllerComponent>(user, out var controller) ||
            controller.Console is not { } consoleUid ||
            !TryComp<XenobiologyConsoleComponent>(consoleUid, out var consoleComp) ||
            _gameTiming.CurTime < controller.NextShortcutTime)
        {
            return;
        }

        controller.NextShortcutTime = _gameTiming.CurTime + consoleComp.ShortcutCooldown;

        if (!TryGetSession(user, out var console, out var remote))
            return;

        if (!TryGetEntity(request.Coordinates.NetEntity, out var coordinateEntity))
            return;

        var coordinates = new EntityCoordinates(coordinateEntity.Value, request.Coordinates.Position);
        if (!IsValidShortcutCoordinates(remote, console.Comp, coordinates))
            return;

        EntityUid? target = null;
        if (request.Target is { } netTarget &&
            TryGetEntity(netTarget, out var entity) &&
            !TerminatingOrDeleted(entity.Value))
        {
            target = entity.Value;
        }

        switch (request.Shortcut)
        {
            case XenobiologyConsoleShortcut.ShiftClick:
                HandleShiftClick(user, console, remote, target, coordinates);
                break;
            case XenobiologyConsoleShortcut.ControlClick:
                HandleControlClick(user, console, remote, target, coordinates);
                break;
        }
    }

    private void HandleShiftClick(
        EntityUid user,
        Entity<XenobiologyConsoleComponent> console,
        EntityUid remote,
        EntityUid? target,
        EntityCoordinates coordinates)
    {
        if (target is { } slimeUid &&
            TryComp<SlimeComponent>(slimeUid, out var slime) &&
            IsValidSlimeTarget(remote, console.Comp, slimeUid))
        {
            TryGrabSlime(user, console, (slimeUid, slime));
            return;
        }

        if (IsFloorTarget(target, coordinates))
            TryReleaseSlimes(user, console, coordinates);
    }

    private void HandleControlClick(
        EntityUid user,
        Entity<XenobiologyConsoleComponent> console,
        EntityUid remote,
        EntityUid? target,
        EntityCoordinates coordinates)
    {
        if (target is { } slimeUid &&
            TryComp<SlimeComponent>(slimeUid, out var slime) &&
            IsValidSlimeTarget(remote, console.Comp, slimeUid))
        {
            AnalyzeSlime(user, (slimeUid, slime));
            return;
        }

        if (target is { } monkey &&
            IsValidMonkeyTarget(remote, console.Comp, monkey))
        {
            TryRecycleMonkey(user, console, monkey);
            return;
        }

        if (IsFloorTarget(target, coordinates))
            TryPlaceMonkey(user, console, coordinates);
    }

    private bool TryPlaceMonkey(
        EntityUid user,
        Entity<XenobiologyConsoleComponent> console,
        EntityCoordinates coordinates)
    {
        if (console.Comp.MonkeyBiomass < console.Comp.MonkeySpawnCost)
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-console-no-biomass"), user, user);
            return false;
        }

        var monkey = Spawn(console.Comp.MonkeyPrototype, coordinates);
        console.Comp.MonkeyBiomass -= console.Comp.MonkeySpawnCost;
        UpdateViewState(console);
        PlayTransferAnimation(console, coordinates, [monkey], XenobiologyTransferAnimationType.Release);
        _popup.PopupEntity(
            Loc.GetString("xenobiology-console-monkey-placed", ("amount", console.Comp.MonkeyBiomass)),
            user,
            user);
        return true;
    }

    private bool TryRecycleMonkey(
        EntityUid user,
        Entity<XenobiologyConsoleComponent> console,
        EntityUid monkey)
    {
        if (!_mobState.IsDead(monkey))
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-console-monkey-alive"), user, user);
            return false;
        }

        var coordinates = Transform(monkey).Coordinates;
        PlayTransferAnimation(console, coordinates, [monkey], XenobiologyTransferAnimationType.Suction);
        QueueDel(monkey);
        console.Comp.MonkeyBiomass += console.Comp.MonkeyRecycleYield;
        UpdateViewState(console);
        _popup.PopupEntity(
            Loc.GetString("xenobiology-console-monkey-recycled", ("amount", console.Comp.MonkeyBiomass)),
            user,
            user);
        return true;
    }

    private bool TryGrabSlime(
        EntityUid user,
        Entity<XenobiologyConsoleComponent> console,
        Entity<SlimeComponent> slime)
    {
        var coordinates = Transform(slime).Coordinates;
        if (_mobState.IsDead(slime) && TrySendToLinkedGrinder(console, slime))
        {
            PlayTransferAnimation(console, coordinates, [slime], XenobiologyTransferAnimationType.Suction);
            _popup.PopupEntity(Loc.GetString("xenobiology-console-slime-sent-to-grinder"), user, user);
            return true;
        }

        if (console.Comp.SlimeContainer.ContainedEntities.Count >= console.Comp.MaxStoredSlimes)
        {
            _popup.PopupEntity(
                Loc.GetString("xenobiology-console-slime-storage-full", ("amount", console.Comp.MaxStoredSlimes)),
                user,
                user);
            return false;
        }

        if (!_container.Insert(slime.Owner, console.Comp.SlimeContainer))
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-console-slime-grab-failed"), user, user);
            return false;
        }

        StoreSlime(slime.Owner, console);
        UpdateViewState(console);
        PlayTransferAnimation(console, coordinates, [slime], XenobiologyTransferAnimationType.Suction);
        _popup.PopupEntity(
            Loc.GetString(
                "xenobiology-console-slime-grabbed",
                ("amount", console.Comp.SlimeContainer.ContainedEntities.Count),
                ("capacity", console.Comp.MaxStoredSlimes)),
            user,
            user);
        return true;
    }

    private bool TrySendToLinkedGrinder(
        Entity<XenobiologyConsoleComponent> console,
        Entity<SlimeComponent> slime)
    {
        if (!TryComp<DeviceLinkSourceComponent>(console, out var source))
        {
            return false;
        }

        foreach (var linked in source.LinkedPorts.Keys)
        {
            if (!_deviceLink.GetLinks(console, linked, source)
                    .Contains((console.Comp.GrinderOutputPort, console.Comp.GrinderInputPort)) ||
                !TryComp<SlimeGrinderComponent>(linked, out var grinder) ||
                !_slimeGrinder.TryQueueProcess(slime, (linked, grinder)))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool TryReleaseSlimes(
        EntityUid user,
        Entity<XenobiologyConsoleComponent> console,
        EntityCoordinates destination)
    {
        var released = ReleaseSlimes(console, destination);
        if (released.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("xenobiology-console-no-stored-slimes"), user, user);
            return false;
        }

        UpdateViewState(console);
        PlayTransferAnimation(console, destination, released, XenobiologyTransferAnimationType.Release);
        _popup.PopupEntity(
            Loc.GetString("xenobiology-console-slimes-released", ("amount", released.Count)),
            user,
            user);
        return true;
    }

    private void AnalyzeSlime(EntityUid user, Entity<SlimeComponent> slime)
    {
        _slimeScanner.SendSlimeScanToChat(user, slime);
    }

    private bool TryGetSession(EntityUid user, out Entity<XenobiologyConsoleComponent> console, out EntityUid remote)
    {
        console = default;
        remote = default;

        if (!TryComp<XenobiologyConsoleControllerComponent>(user, out var controller) ||
            controller.Console is not { } consoleUid ||
            controller.RemoteEntity is not { } remoteUid ||
            !TryComp<XenobiologyConsoleComponent>(consoleUid, out var consoleComp) ||
            Terminating(consoleUid) ||
            Terminating(remoteUid))
        {
            return false;
        }

        console = (consoleUid, consoleComp);
        remote = remoteUid;

        if (!IsSessionValid(user, controller))
        {
            StopControl((user, controller), removeController: true);
            console = default;
            remote = default;
            return false;
        }

        if (!TryRefreshCameraCoverage((user, controller), console, Transform(remote).Coordinates))
        {
            StopControl((user, controller), removeController: true);
            console = default;
            remote = default;
            return false;
        }

        return true;
    }

    private bool TryFindSlime(
        EntityUid remote,
        XenobiologyConsoleComponent console,
        Func<Entity<SlimeComponent>, bool> predicate,
        out Entity<SlimeComponent> slime)
    {
        var closestDistance = float.MaxValue;
        slime = default;

        foreach (var (candidate, distance) in FindSlimesWithDistance(remote, console, predicate))
        {
            if (distance >= closestDistance)
                continue;

            closestDistance = distance;
            slime = candidate;
        }

        return closestDistance < float.MaxValue;
    }

    private IEnumerable<(Entity<SlimeComponent> Slime, float Distance)> FindSlimesWithDistance(
        EntityUid remote,
        XenobiologyConsoleComponent console,
        Func<Entity<SlimeComponent>, bool> predicate)
    {
        var coords = Transform(remote).Coordinates;
        foreach (var candidate in _lookup.GetEntitiesInRange<SlimeComponent>(
                     coords,
                     console.SlimeTargetRange,
                     LookupFlags.Dynamic | LookupFlags.Sundries | LookupFlags.Approximate))
        {
            if (!predicate(candidate) ||
                !TryFindVisibleCamera(candidate, console, out _) ||
                !coords.TryDistance(EntityManager, Transform(candidate).Coordinates, out var distance))
            {
                continue;
            }

            yield return (candidate, distance);
        }
    }

    private bool TryFindMonkey(EntityUid remote, XenobiologyConsoleComponent console, out EntityUid monkey)
    {
        var coords = Transform(remote).Coordinates;
        var closestDistance = float.MaxValue;
        monkey = default;

        foreach (var candidate in _lookup.GetEntitiesInRange(coords, console.InteractionRange, LookupFlags.Dynamic | LookupFlags.Approximate))
        {
            if (MetaData(candidate).EntityPrototype?.ID != console.MonkeyPrototype.Id ||
                !TryFindVisibleCamera(candidate, console, out _) ||
                !coords.TryDistance(EntityManager, Transform(candidate).Coordinates, out var distance) ||
                distance >= closestDistance)
            {
                continue;
            }

            closestDistance = distance;
            monkey = candidate;
        }

        return closestDistance < float.MaxValue;
    }

    private bool IsValidSlimeTarget(
        EntityUid remote,
        XenobiologyConsoleComponent console,
        EntityUid slime)
    {
        var remoteCoordinates = Transform(remote).Coordinates;
        return HasComp<SlimeComponent>(slime) &&
               !_container.IsEntityInContainer(slime) &&
               remoteCoordinates.TryDistance(EntityManager, Transform(slime).Coordinates, out var distance) &&
               distance <= console.SlimeTargetRange &&
               TryFindVisibleCamera(slime, console, out _);
    }

    private bool IsValidMonkeyTarget(
        EntityUid remote,
        XenobiologyConsoleComponent console,
        EntityUid monkey)
    {
        var remoteCoordinates = Transform(remote).Coordinates;
        return MetaData(monkey).EntityPrototype?.ID == console.MonkeyPrototype.Id &&
               !_container.IsEntityInContainer(monkey) &&
               remoteCoordinates.TryDistance(EntityManager, Transform(monkey).Coordinates, out var distance) &&
               distance <= console.InteractionRange &&
               TryFindVisibleCamera(monkey, console, out _);
    }

    private bool IsValidShortcutCoordinates(
        EntityUid remote,
        XenobiologyConsoleComponent console,
        EntityCoordinates coordinates)
    {
        var maxRange = MathF.Max(console.InteractionRange, console.SlimeTargetRange);
        return coordinates.IsValid(EntityManager) &&
               Transform(remote).Coordinates.TryDistance(EntityManager, coordinates, out var distance) &&
               distance <= maxRange &&
               TryFindVisibleCamera(coordinates, console, out _);
    }

    private static bool IsFloorTarget(EntityUid? target, EntityCoordinates coordinates)
    {
        return target == null || target == coordinates.EntityId;
    }

    private bool IsSessionValid(EntityUid user, XenobiologyConsoleControllerComponent controller)
    {
        if (controller.Console is not { } consoleUid ||
            controller.RemoteEntity is not { } remoteUid ||
            !TryComp<XenobiologyConsoleComponent>(consoleUid, out var console) ||
            !TryComp<XenobiologyConsoleRemoteComponent>(remoteUid, out var remote) ||
            Terminating(consoleUid) ||
            Terminating(remoteUid) ||
            Terminating(user) ||
            console.ActiveUser != user ||
            console.RemoteEntity != remoteUid ||
            remote.Controller != user ||
            !HasConsoleAccess(user, consoleUid) ||
            !IsConsolePowered((consoleUid, console)) ||
            !IsUserInRange(user, controller, (consoleUid, console)))
        {
            return false;
        }

        return true;
    }

    private bool IsConsolePowered(Entity<XenobiologyConsoleComponent> console)
    {
        return !TryComp<ApcPowerReceiverComponent>(console, out var power) || power.Powered;
    }

    private bool HasConsoleAccess(EntityUid user, EntityUid console)
    {
        return !TryComp<AccessReaderComponent>(console, out var access) ||
               _access.IsAllowed(user, console, access);
    }

    private bool IsUserInRange(
        EntityUid user,
        XenobiologyConsoleControllerComponent controller,
        Entity<XenobiologyConsoleComponent> console)
    {
        var origin = user;
        if (controller.PreviousRelayEntity is { } relay && !TerminatingOrDeleted(relay))
            origin = relay;

        return Transform(origin).Coordinates.TryDistance(EntityManager, Transform(console).Coordinates, out var distance) &&
               distance <= console.Comp.UserMaxDistance;
    }

    private TimeSpan GetValidationInterval(XenobiologyConsoleControllerComponent controller)
    {
        if (controller.Console is { } consoleUid &&
            TryComp<XenobiologyConsoleComponent>(consoleUid, out var console))
        {
            return console.SessionValidationInterval;
        }

        return TimeSpan.FromSeconds(0.5);
    }

    private void StoreSlime(EntityUid slime, Entity<XenobiologyConsoleComponent> console)
    {
        var stored = EnsureComp<XenobiologyConsoleStoredSlimeComponent>(slime);
        stored.Console = console;

        if (TryComp<HTNComponent>(slime, out var htn))
        {
            stored.HtnWasEnabled ??= htn.Enabled;
            _htn.SetHTNEnabled((slime, htn), false);
        }

        _npc.SleepNPC(slime);
        _actionBlocker.UpdateCanMove(slime);
    }

    private List<EntityUid> ReleaseSlimes(
        Entity<XenobiologyConsoleComponent> console,
        EntityCoordinates destination)
    {
        var released = new List<EntityUid>(console.Comp.SlimeContainer.ContainedEntities.Count);
        foreach (var slime in _container.EmptyContainer(console.Comp.SlimeContainer, destination: destination))
            released.Add(slime);

        return released;
    }

    private void PlayTransferAnimation(
        Entity<XenobiologyConsoleComponent> console,
        EntityCoordinates coordinates,
        List<EntityUid> targets,
        XenobiologyTransferAnimationType type)
    {
        if (targets.Count == 0)
            return;

        var sound = type == XenobiologyTransferAnimationType.Suction
            ? console.Comp.SuctionSound
            : console.Comp.ReleaseSound;
        _audio.PlayPvs(sound, coordinates);

        var animation = new XenobiologyTransferAnimationEvent(
            GetNetCoordinates(coordinates),
            GetNetEntityList(targets),
            type);
        RaiseNetworkEvent(animation, Filter.Pvs(coordinates, entityMan: EntityManager));
    }

    private void UpdateViewState(
        Entity<XenobiologyConsoleComponent> console,
        Entity<XenobiologyConsoleViewComponent>? viewEntity = null)
    {
        if (viewEntity == null)
        {
            if (console.Comp.ActiveUser is not { } user ||
                !TryComp<XenobiologyConsoleViewComponent>(user, out var view))
            {
                return;
            }

            viewEntity = (user, view);
        }

        var viewComp = viewEntity.Value.Comp;
        viewComp.StoredSlimes = console.Comp.SlimeContainer.ContainedEntities.Count;
        viewComp.MaxStoredSlimes = console.Comp.MaxStoredSlimes;
        viewComp.MonkeyBiomass = console.Comp.MonkeyBiomass;
        Dirty(viewEntity.Value.Owner, viewComp);
    }

    private void RestoreStoredSlime(EntityUid slime)
    {
        if (!TryComp<XenobiologyConsoleStoredSlimeComponent>(slime, out var stored))
            return;

        if (TryComp<HTNComponent>(slime, out var htn))
        {
            _htn.SetHTNEnabled((slime, htn), stored.HtnWasEnabled ?? true);
            if (htn.Enabled && !_mobState.IsDead(slime))
                _npc.WakeNPC(slime, htn);
        }

        RemComp<XenobiologyConsoleStoredSlimeComponent>(slime);
        _actionBlocker.UpdateCanMove(slime);
    }

    private void OnStoredSlimeMoveAttempt(Entity<XenobiologyConsoleStoredSlimeComponent> ent, ref UpdateCanMoveEvent args)
    {
        args.Cancel();
    }

    private void OnStoredSlimeAttackAttempt(Entity<XenobiologyConsoleStoredSlimeComponent> ent, ref AttackAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnStoredSlimeUseAttempt(Entity<XenobiologyConsoleStoredSlimeComponent> ent, ref UseAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnStoredSlimePullAttempt(Entity<XenobiologyConsoleStoredSlimeComponent> ent, ref StartPullAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnStoredSlimeShutdown(Entity<XenobiologyConsoleStoredSlimeComponent> ent, ref ComponentShutdown args)
    {
        if (Terminating(ent))
            return;

        if (TryComp<HTNComponent>(ent, out var htn) && ent.Comp.HtnWasEnabled is { } wasEnabled)
            _htn.SetHTNEnabled((ent.Owner, htn), wasEnabled);
    }

    private void StopControlIfSessionInvalid(Entity<XenobiologyConsoleControllerComponent> controller)
    {
        if (TerminatingOrDeleted(controller))
            return;

        if (!IsSessionValid(controller, controller.Comp))
            StopControl(controller, removeController: true);
    }
}
