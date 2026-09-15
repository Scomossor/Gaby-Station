// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Server.WhiteDream.BloodCult.Runes.Rending;

[RegisterComponent]
public sealed partial class CultRuneRendingComponent : Component
{
    [DataField]
    public SoundSpecifier FinishedDrawingAudio =
        new SoundPathSpecifier("/Audio/WhiteDream/BloodCult/rending_draw_finished.ogg");

    [DataField]
    public SoundSpecifier SummonAudio = new SoundPathSpecifier("/Audio/WhiteDream/BloodCult/rending_ritual.ogg");

    #region Collective chant

    /// <summary>
    ///     How many times the cult has to chant together before the veil gives.
    /// </summary>
    [DataField]
    public int TotalChantSteps = 6;

    /// <summary>
    ///     Seconds between each chant.
    /// </summary>
    [DataField]
    public float ChantInterval = 5f;

    /// <summary>
    ///     How close a cultist has to stand to a rending rune to count as chanting.
    /// </summary>
    [DataField]
    public float ParticipantRange = 1.5f;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool RitualInProgress;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool RitualCompleted;

    [ViewVariables(VVAccess.ReadOnly)]
    public int CurrentChantStep;

    [ViewVariables(VVAccess.ReadOnly)]
    public float TimeUntilNextChant;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? RitualMap;

    #endregion
}
