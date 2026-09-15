// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Audio;
using Content.Shared._ES.DeathCutscene;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._ES.DeathCutscene;

public sealed class DeathCutsceneSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ContentAudioSystem _contentAudio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;

    private DeathCutsceneOverlay? _current;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<PlayDeathCutsceneEvent>(OnPlayDeathCutscene);
        SubscribeNetworkEvent<StopDeathCutsceneEvent>(OnStopDeathCutscene);
    }

    public override void FrameUpdate(float frameTime)
    {
        if (_current is { Finished: true })
            RemoveOverlay();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        RemoveOverlay();
    }

    private void OnPlayDeathCutscene(PlayDeathCutsceneEvent msg)
    {
        if (_current != null)
            return;

        _current = new DeathCutsceneOverlay(msg.Timings, _timing.RealTime);
        _overlay.AddOverlay(_current);

        if (msg.SuppressAmbientMusic)
            _contentAudio.SetAmbientMusicSuppressed(true);

        _audio.PlayGlobal(msg.Sound, Filter.Local(), false);
    }

    private void OnStopDeathCutscene(StopDeathCutsceneEvent msg)
    {
        RemoveOverlay();
    }

    private void RemoveOverlay()
    {
        if (_current == null)
            return;

        _overlay.RemoveOverlay(_current);
        _current = null;

        _contentAudio.SetAmbientMusicSuppressed(false);
    }
}
