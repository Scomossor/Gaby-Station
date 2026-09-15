// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Client.Audio;

public sealed partial class ContentAudioSystem
{
    public bool AmbientMusicSuppressed { get; private set; }

    public void SetAmbientMusicSuppressed(bool suppressed)
    {
        AmbientMusicSuppressed = suppressed;

        if (suppressed)
            DisableAmbientMusic();
    }
}
