// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.DeathCutscene;

[RegisterComponent, NetworkedComponent]
public sealed partial class DeathCutsceneComponent : Component
{
    [DataField]
    public TimeSpan DesaturationDuration = TimeSpan.FromSeconds(8);

    [DataField]
    public TimeSpan BlackoutDelay = TimeSpan.FromSeconds(8.5);

    [DataField]
    public TimeSpan BlackoutFadeInDuration = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan BlackoutHoldDuration = TimeSpan.FromSeconds(0.75);

    [DataField]
    public TimeSpan BlackoutFadeOutDuration = TimeSpan.FromSeconds(1.5);

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public bool SuppressAmbientMusic = true;

    [DataField]
    public EntProtoId EyePrototype = "ESDeathCutsceneEye";

    [DataField]
    public bool CanReturnToBody = true;

    public DeathCutsceneTimings GetTimings()
    {
        return new DeathCutsceneTimings(DesaturationDuration,
            BlackoutDelay,
            BlackoutFadeInDuration,
            BlackoutHoldDuration,
            BlackoutFadeOutDuration);
    }
}
