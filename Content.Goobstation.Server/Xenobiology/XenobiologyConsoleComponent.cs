// SPDX-FileCopyrightText: 2026 Project Dumont
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Actions.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.Xenobiology;

[RegisterComponent]
public sealed partial class XenobiologyConsoleComponent : Component
{
    public const string SlimeContainerId = "xenobiology-console-slimes";

    [DataField]
    public EntProtoId RemoteEntityPrototype = "XenobiologyConsoleEye";

    [DataField]
    public EntProtoId MonkeyPrototype = "MobMonkey";

    [DataField]
    public ProtoId<TagPrototype> MonkeyCubeTag = "MonkeyCube";

    [DataField]
    public float InteractionRange = 1.5f;

    [DataField]
    public float SlimeTargetRange = 1.25f;

    [DataField]
    public float UserMaxDistance = 2f;

    [DataField]
    public TimeSpan SessionValidationInterval = TimeSpan.FromSeconds(0.5);

    [DataField]
    public TimeSpan ShortcutCooldown = TimeSpan.FromSeconds(0.1);

    [DataField]
    public float CameraSearchRange = 128f;

    [DataField]
    public string RequiredCameraTag = "Xenobiology";

    [DataField]
    public float CameraOverlaySearchRange = 16f;

    [DataField]
    public FixedPoint2 MonkeyCubeBiomass = 1;

    [DataField]
    public FixedPoint2 MonkeySpawnCost = 1;

    [DataField]
    public FixedPoint2 MonkeyRecycleYield = 0.2;

    [DataField]
    public SoundSpecifier SuctionSound = new SoundPathSpecifier("/Audio/_Goobstation/Xenobiology/air_suck.ogg");

    [DataField]
    public SoundSpecifier ReleaseSound = new SoundPathSpecifier("/Audio/_Goobstation/Xenobiology/air_shoot.ogg");

    [DataField]
    public int MaxStoredSlimes = 5;

    [DataField]
    public ProtoId<SourcePortPrototype> GrinderOutputPort = "XenobiologySlimeTransfer";

    [DataField]
    public ProtoId<SinkPortPrototype> GrinderInputPort = "XenobiologySlimeReceiver";

    [ViewVariables]
    public FixedPoint2 MonkeyBiomass;

    [ViewVariables]
    public Container SlimeContainer = default!;

    [ViewVariables]
    public EntityUid? ActiveUser;

    [ViewVariables]
    public EntityUid? RemoteEntity;
}

[RegisterComponent]
public sealed partial class XenobiologyConsoleControllerComponent : Component
{
    [DataField]
    public EntProtoId<ActionComponent> ExitAction = "ActionXenobiologyConsoleExit";

    [DataField]
    public EntProtoId<ActionComponent> PlaceMonkeyAction = "ActionXenobiologyConsolePlaceMonkey";

    [DataField]
    public EntProtoId<ActionComponent> RecycleMonkeyAction = "ActionXenobiologyConsoleRecycleMonkey";

    [DataField]
    public EntProtoId<ActionComponent> GrabSlimeAction = "ActionXenobiologyConsoleGrabSlime";

    [DataField]
    public EntProtoId<ActionComponent> ReleaseSlimesAction = "ActionXenobiologyConsoleReleaseSlimes";

    [DataField]
    public EntProtoId<ActionComponent> AnalyzeSlimeAction = "ActionXenobiologyConsoleAnalyzeSlime";

    [DataField]
    public EntProtoId<ActionComponent> ShowShortcutsAction = "ActionXenobiologyConsoleShowShortcuts";

    [ViewVariables]
    public EntityUid? Console;

    [ViewVariables]
    public EntityUid? RemoteEntity;

    [ViewVariables]
    public EntityUid? ExitActionEntity;

    [ViewVariables]
    public EntityUid? PlaceMonkeyActionEntity;

    [ViewVariables]
    public EntityUid? RecycleMonkeyActionEntity;

    [ViewVariables]
    public EntityUid? GrabSlimeActionEntity;

    [ViewVariables]
    public EntityUid? ReleaseSlimesActionEntity;

    [ViewVariables]
    public EntityUid? AnalyzeSlimeActionEntity;

    [ViewVariables]
    public EntityUid? ShowShortcutsActionEntity;

    [ViewVariables]
    public TimeSpan NextValidationTime;

    [ViewVariables]
    public TimeSpan NextShortcutTime;

    [ViewVariables]
    public EntityUid? ActiveCamera;

    [ViewVariables]
    public EntityCoordinates? LastValidCoordinates;

    [ViewVariables]
    public EntityUid? PreviousEyeTarget;

    [ViewVariables]
    public EntityUid? PreviousRelayEntity;

    [ViewVariables]
    public bool? PreviousDrawFov;

    [ViewVariables]
    public bool? PreviousDrawLight;

    [ViewVariables]
    public bool CleaningUp;
}
