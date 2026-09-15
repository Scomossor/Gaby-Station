// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.DeathCutscene;

[Serializable, NetSerializable]
public sealed class PlayDeathCutsceneEvent(DeathCutsceneTimings timings, SoundSpecifier? sound, bool suppressAmbientMusic)
    : EntityEventArgs
{
    public readonly DeathCutsceneTimings Timings = timings;
    public readonly SoundSpecifier? Sound = sound;
    public readonly bool SuppressAmbientMusic = suppressAmbientMusic;
}

[Serializable, NetSerializable]
public sealed class StopDeathCutsceneEvent : EntityEventArgs;
