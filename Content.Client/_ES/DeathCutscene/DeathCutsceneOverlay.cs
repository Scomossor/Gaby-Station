// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._ES.DeathCutscene;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._ES.DeathCutscene;

public sealed class DeathCutsceneOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> ShaderProto = "ESDeathCutscene";

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace | OverlaySpace.ScreenSpace;
    public override bool RequestScreenTexture => true;

    private readonly ShaderInstance _shader;
    private readonly TimeSpan _startTime;
    private readonly DeathCutsceneTimings _timings;

    public bool Finished => Elapsed >= _timings.Duration;

    private TimeSpan Elapsed => _timing.RealTime - _startTime;

    public DeathCutsceneOverlay(DeathCutsceneTimings timings, TimeSpan startTime)
    {
        IoCManager.InjectDependencies(this);

        _shader = _prototype.Index(ShaderProto).InstanceUnique();
        _timings = timings;
        _startTime = startTime;
        ZIndex = 9;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        switch (args.Space)
        {
            case OverlaySpace.WorldSpace:
                DrawDesaturation(args);
                break;
            case OverlaySpace.ScreenSpace:
                DrawBlackout(args);
                break;
        }
    }

    private void DrawDesaturation(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var intensity = GetDesaturation();
        if (intensity <= 0f)
            return;

        var handle = args.WorldHandle;
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("intensity", intensity);
        _shader.SetParameter("noiseIntensity", intensity);

        handle.UseShader(_shader);
        handle.DrawRect(args.WorldBounds, Color.White);
        handle.UseShader(null);
    }

    private void DrawBlackout(in OverlayDrawArgs args)
    {
        var alpha = GetBlackoutAlpha();
        if (alpha <= 0f)
            return;

        args.ScreenHandle.DrawRect(args.ViewportBounds, Color.Black.WithAlpha(alpha));
    }

    private float GetDesaturation()
    {
        return Easings.OutSine(Progress(TimeSpan.Zero, _timings.DesaturationDuration));
    }

    private float GetBlackoutAlpha()
    {
        var fadeOutStart = _timings.GhostDelay + _timings.BlackoutHoldDuration;
        if (Elapsed < fadeOutStart)
            return Easings.InSine(Progress(_timings.BlackoutDelay, _timings.BlackoutFadeInDuration));

        return 1f - Easings.OutSine(Progress(fadeOutStart, _timings.BlackoutFadeOutDuration));
    }

    private float Progress(TimeSpan start, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            return Elapsed >= start ? 1f : 0f;

        return Math.Clamp((float)((Elapsed - start) / duration), 0f, 1f);
    }
}
