// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server.Shuttles.Events;

/// <summary>
/// Raised when the emergency shuttle docks to the station.
/// </summary>
[ByRefEvent]
public record struct EmergencyShuttleDockedEvent();
