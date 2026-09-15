// SPDX-FileCopyrightText: 2026 Project Dumont
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Goobstation.Maths.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Xenobiology.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class XenobiologyConsoleViewComponent : Component
{
    [DataField, AutoNetworkedField]
    public string RequiredCameraTag = "Xenobiology";

    [DataField, AutoNetworkedField]
    public float CameraOverlaySearchRange = 16f;

    [AutoNetworkedField]
    public int StoredSlimes;

    [AutoNetworkedField]
    public int MaxStoredSlimes = 5;

    [AutoNetworkedField]
    public FixedPoint2 MonkeyBiomass;
}

public sealed partial class XenobiologyConsoleExitEvent : InstantActionEvent;

public sealed partial class XenobiologyConsolePlaceMonkeyEvent : InstantActionEvent;

public sealed partial class XenobiologyConsoleRecycleMonkeyEvent : InstantActionEvent;

public sealed partial class XenobiologyConsoleGrabSlimeEvent : InstantActionEvent;

public sealed partial class XenobiologyConsoleReleaseSlimesEvent : InstantActionEvent;

public sealed partial class XenobiologyConsoleAnalyzeSlimeEvent : InstantActionEvent;

public sealed partial class XenobiologyConsoleShowShortcutsEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed class XenobiologyConsoleShortcutRequest(
    XenobiologyConsoleShortcut shortcut,
    NetEntity? target,
    NetCoordinates coordinates) : EntityEventArgs
{
    public XenobiologyConsoleShortcut Shortcut = shortcut;
    public NetEntity? Target = target;
    public NetCoordinates Coordinates = coordinates;
}

[Serializable, NetSerializable]
public enum XenobiologyConsoleShortcut : byte
{
    ShiftClick,
    ControlClick,
}
