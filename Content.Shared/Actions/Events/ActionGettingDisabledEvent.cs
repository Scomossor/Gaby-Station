// SPDX-License-Identifier: AGPL-3.0-or-later
// Blood Cult: ported from WWhiteDreamProject/wwdpublic. See Content.Shared/WhiteDream/BloodCult/ATTRIBUTION.md

namespace Content.Shared.Actions.Events;

/// <summary>
///     Raised on the action entity when it is getting disabled on <see cref="Performer">performer</see>.
/// </summary>
/// <param name="Performer">The entity that performed this action.</param>
[ByRefEvent]
public readonly record struct ActionGettingDisabledEvent(EntityUid Performer);
