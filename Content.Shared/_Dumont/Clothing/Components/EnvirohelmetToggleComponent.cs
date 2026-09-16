using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Dumont.Clothing.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EnvirohelmetToggleComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsActive;

    [DataField]
    public EntProtoId Action = "ActionToggleEnvirohelmet";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public SoundSpecifier? ToggleSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    [DataField(required: true)]
    public ComponentRegistry Components = new();

    [DataField]
    public ComponentRegistry ClosedComponents = new();

    [DataField]
    public ComponentRegistry? RemoveComponents;

    [DataField]
    public bool Parent;

    [DataField]
    public EntityUid? Target;

    [DataField, AutoNetworkedField]
    public EntityUid? WearerEntity;

    [DataField, AutoNetworkedField]
    public TimeSpan ActionTime = TimeSpan.FromSeconds(0.5);
}

public sealed partial class ToggleEnvirohelmetEvent : InstantActionEvent
{
    [Serializable, NetSerializable]
    public enum ToggleableVisuals : byte
    {
        Enabled,
        Layer
    }

    [Serializable, NetSerializable]
    public enum LightLayers : byte
    {
        Light,
        Unshaded,
    }
}

[Serializable, NetSerializable]
public enum EnvirohelmetVisuals : byte
{
    IsOpen
}
