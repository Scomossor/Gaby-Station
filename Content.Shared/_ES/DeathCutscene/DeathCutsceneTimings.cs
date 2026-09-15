// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared._ES.DeathCutscene;

[Serializable, NetSerializable]
public readonly record struct DeathCutsceneTimings(
    TimeSpan DesaturationDuration,
    TimeSpan BlackoutDelay,
    TimeSpan BlackoutFadeInDuration,
    TimeSpan BlackoutHoldDuration,
    TimeSpan BlackoutFadeOutDuration)
{
    public TimeSpan GhostDelay => BlackoutDelay + BlackoutFadeInDuration;

    public TimeSpan Duration => GhostDelay + BlackoutHoldDuration + BlackoutFadeOutDuration;
}
