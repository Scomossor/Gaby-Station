// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.WhiteDream.BloodCult.BloodRites;

/// <summary>
///     Raised when a cultist finishes draining blood out of a restrained victim.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BloodRitesDrainDoAfterEvent : SimpleDoAfterEvent;
