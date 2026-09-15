// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.WhiteDream.BloodCult.Runes;

[Serializable, NetSerializable]
public sealed partial class RendingRuneDoAfter : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public enum RendingRuneVisuals
{
    Active,
    Layer
}
