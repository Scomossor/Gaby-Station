// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Body.Events;

/// <summary>
/// Raised on an entity that is about to suffocate. Cancelling it skips the gasp and the damage.
/// </summary>
[ByRefEvent]
public record struct SuffocationBeforeEvent(bool Cancelled = false);
