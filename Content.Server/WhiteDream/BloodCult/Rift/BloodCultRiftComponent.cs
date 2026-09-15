// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.WhiteDream.BloodCult.Rift;

/// <summary>
///     The bleeding wound in reality that opens once the veil is weakened.
///     Cultists chant on the runes around it to drag Nar'Sie through.
/// </summary>
[RegisterComponent]
public sealed partial class BloodCultRiftComponent : Component
{
    public const string SolutionName = "sanguine_pool";
    public static readonly ProtoId<ReagentPrototype> Reagent = "SanguinePerniculate";

    #region Bleeding

    [DataField]
    public float PulseInterval = 30f;

    [DataField]
    public float BloodPerPulse = 50f;

    [ViewVariables(VVAccess.ReadOnly)]
    public float TimeUntilNextPulse;

    #endregion

    #region Ritual

    /// <summary>
    ///     The offering runes placed around the rift when it spawned.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> SummoningRunes = new();

    [DataField]
    public float RuneRange = 1.5f;

    /// <summary>
    ///     How many cultists have to be standing on the runes. Drops by one after every sacrifice,
    ///     since the sacrifice was one of them.
    /// </summary>
    [DataField]
    public int RequiredCultists = 3;

    /// <summary>
    ///     How many cultists Nar'Sie eats before she comes through herself.
    /// </summary>
    [DataField]
    public int RequiredSacrifices = 3;

    /// <summary>
    ///     Seconds between chants before the first offering. Slow, sparse, ominous.
    ///     Each cycle below is tuned so the whole ritual runs about as long as the music.
    /// </summary>
    [DataField]
    public List<float> ChantDelaysFirst = new() { 16f, 15f, 14f, 12f, 11f, 10f };

    /// <summary>
    ///     After the first offering the cult finds its rhythm.
    /// </summary>
    [DataField]
    public List<float> ChantDelaysSecond = new() { 9f, 8f, 7.5f, 7f, 6.5f, 6f, 5f, 4.5f, 4f };

    /// <summary>
    ///     After the second offering it becomes a frenzy.
    /// </summary>
    [DataField]
    public List<float> ChantDelaysThird = new()
    {
        4f, 3.6f, 3.3f, 3f, 2.8f, 2.6f, 2.4f, 2.2f,
        2f, 1.8f, 1.6f, 1.4f, 1.2f, 1f, 0.9f, 0.8f,
    };

    /// <summary>
    ///     The chant cycle for however many offerings have already been made.
    /// </summary>
    public List<float> CurrentCycle => SacrificesDone switch
    {
        0 => ChantDelaysFirst,
        1 => ChantDelaysSecond,
        _ => ChantDelaysThird,
    };

    /// <summary>
    ///     How many words each cycle puts in a follower's mouth. It builds.
    /// </summary>
    public int FollowerChantWords => SacrificesDone switch
    {
        0 => 1,
        1 => 2,
        _ => 3,
    };

    /// <summary>
    ///     The one marked for the veil always speaks longer than everyone else.
    /// </summary>
    public int LeaderChantWords => FollowerChantWords + 3;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool RitualInProgress;

    [ViewVariables(VVAccess.ReadOnly)]
    public int ChantsInCycle;

    [ViewVariables(VVAccess.ReadOnly)]
    public int SacrificesDone;

    /// <summary>
    ///     The participant requirement at the beginning of the current attempt. Sacrifices lower
    ///     <see cref="RequiredCultists"/>, so an aborted ritual needs this exact value to reset.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public int? RitualStartingRequiredCultists;

    [ViewVariables(VVAccess.ReadOnly)]
    public float TimeUntilNextChant;

    /// <summary>
    ///     Whoever is speaking the long chant is the next to die.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? PendingSacrifice;

    #endregion

    #region Guardians

    /// <summary>
    ///     What the rift vomits out to defend itself. Without this the rift is a passive object the
    ///     crew can simply walk up to and camp.
    /// </summary>
    [DataField]
    public EntProtoId GuardianProto = "MobHellspawnCult";

    /// <summary>
    ///     Seconds before a new guardian crawls out, while the rift is just sitting there bleeding.
    /// </summary>
    [DataField]
    public float GuardianInterval = 60f;

    /// <summary>
    ///     Seconds before a replacement crawls out once the final chant has begun. The veil is wide
    ///     open by then, so the rift patches its own guard faster.
    /// </summary>
    [DataField]
    public float RitualGuardianInterval = 25f;

    /// <summary>
    ///     One at a time. A hellspawn is a mini-boss on its own; a pack of them is not a fight.
    /// </summary>
    [DataField]
    public int MaxGuardians = 1;

    [DataField]
    public int RitualMaxGuardians = 1;

    /// <summary>
    ///     How many guardians the rift spawns over the whole round.
    /// </summary>
    [DataField]
    public int TotalGuardians = 1;

    [ViewVariables(VVAccess.ReadOnly)]
    public int GuardiansSpawned;

    /// <summary>
    ///     How far from the rift a guardian can crawl out.
    /// </summary>
    [DataField]
    public float GuardianSpawnRange = 2.5f;

    [ViewVariables(VVAccess.ReadOnly)]
    public float TimeUntilNextGuardian;

    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> Guardians = new();

    #endregion

    #region Music

    /// <summary>
    ///     Roughly 174 seconds long. The chant cycles above are tuned to finish with it.
    /// </summary>
    [DataField]
    public SoundSpecifier RitualMusic = new SoundPathSpecifier("/Audio/WhiteDream/BloodCult/tear_of_veil.ogg");

    [ViewVariables(VVAccess.ReadOnly)]
    public bool MusicPlaying;

    #endregion

    // Dumont changes start
    [DataField]
    public SoundSpecifier SummonSound = new SoundPathSpecifier("/Audio/WhiteDream/BloodCult/narsie_summoned.ogg");

    [DataField]
    public EntProtoId SacrificeLightningProto = "CultSacrificeLightning";

    [DataField]
    public TimeSpan SacrificeFlickerTime = TimeSpan.FromSeconds(3);

    /// <summary>
    ///     Raised on the station the moment Nar'Sie takes the first of the chanters. Delta was
    ///     already called when the veil tore; this is the code for what is coming through it.
    /// </summary>
    [DataField]
    public string FirstSacrificeAlertLevel = "octarine"; // Dumont
    // Dumont end

    [DataField]
    public EntProtoId NarsiePrototype = "MobNarsieSpawn";

    /// <summary>
    ///     What the offered cultists come back as. Nar'Sie's heralds, not soulstones.
    /// </summary>
    [DataField]
    public EntProtoId HarvesterProto = "ConstructHarvester";

    [DataField]
    public EntProtoId SoulShardProto = "SoulShard";

    [DataField]
    public EntProtoId SoulShardGhostProto = "SoulShardGhost";
}

/// <summary>
///     Marks the runes around a rift so invoking them starts the final summoning.
/// </summary>
[RegisterComponent]
public sealed partial class FinalSummoningRuneComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Rift;
}
