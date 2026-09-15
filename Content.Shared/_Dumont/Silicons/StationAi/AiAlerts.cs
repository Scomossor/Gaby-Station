// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._Dumont.Silicons.StationAi;

/// <summary>
/// abre o monitor interno da IA.
/// </summary>
public sealed partial class ToggleAiAlertsScreenEvent : InstantActionEvent;

/// <summary>
/// marca um ponto pro coletivo silicon
/// </summary>
public sealed partial class AiPlaceWaypointEvent : WorldTargetActionEvent;

[Serializable, NetSerializable]
public enum AiAlertsUiKey : byte
{
    Key
}

/// <summary>
/// a ordem importa, o monitor lista do pior pro melhor
/// </summary>
[Serializable, NetSerializable]
public enum AiAlertSeverity : byte
{
    Info,
    Warning,
    Danger,
}

[Serializable, NetSerializable]
public enum AiAlertKind : byte
{
    Atmos,
    Fire,
    Door,
    Waypoint,
}

/// <summary>
/// uma linha viva do monitor interno da IA
/// </summary>
[Serializable, NetSerializable]
public record struct AiAlertEntry()
{
    /// <summary>
    /// o alarme, a porta ou o marcador. é pra onde o botão Ir leva
    /// </summary>
    public NetEntity Source = NetEntity.Invalid;

    public string Area = string.Empty;

    /// <summary>
    /// quem está pedindo. só é preenchido em <see cref="AiAlertKind.Door"/>.
    /// </summary>
    public string Subject = string.Empty;

    public AiAlertKind Kind;

    public AiAlertSeverity Severity;
}

[Serializable, NetSerializable]
public sealed class AiAlertsBuiState : BoundUserInterfaceState
{
    public List<AiAlertEntry> Alerts;

    public AiAlertsBuiState(List<AiAlertEntry> alerts)
    {
        Alerts = alerts;
    }
}

/// <summary>
/// enviada quando a IA aperta o botão Ir de uma linha
/// </summary>
[Serializable, NetSerializable]
public sealed class AiAlertWarpMessage : BoundUserInterfaceMessage
{
    public NetEntity Target;

    public AiAlertWarpMessage(NetEntity target)
    {
        Target = target;
    }
}

/// <summary>
/// enviada quando a IA oculta uma linha do monitor
/// </summary>
[Serializable, NetSerializable]
public sealed class AiAlertDismissMessage : BoundUserInterfaceMessage
{
    public NetEntity Target;

    public AiAlertDismissMessage(NetEntity target)
    {
        Target = target;
    }
}

[Serializable, NetSerializable]
public sealed class AiAlertsRefreshMessage : BoundUserInterfaceMessage;
