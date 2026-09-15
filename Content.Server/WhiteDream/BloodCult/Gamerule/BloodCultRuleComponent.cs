// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.WhiteDream.BloodCult.RendingRunePlacement;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Content.Shared.WhiteDream.BloodCult.Constructs;
using Robust.Shared.Prototypes;

namespace Content.Server.WhiteDream.BloodCult.Gamerule;

[RegisterComponent]
public sealed partial class BloodCultRuleComponent : Component
{
    [DataField]
    public ProtoId<NpcFactionPrototype> NanoTrasenFaction = "NanoTrasen";

    [DataField]
    public ProtoId<NpcFactionPrototype> BloodCultFaction = "GeometerOfBlood";

    [DataField]
    public EntProtoId HarvesterPrototype = "ConstructHarvester";

    [DataField]
    public Color EyeColor = Color.FromHex("#f80000");

    // Dumont
    [DataField]
    public float ReadEyeThreshold = 0.1f;

    [DataField]
    public float PentagramThreshold = 0.2f;

    /// <summary>
    ///     Crew count captured when the round starts. Percentage requirements use it as the denominator.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public int ProgressionCrewCount;

    /// <summary>
    ///     Whether the final reckoning spell was already granted this round.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool FinalReckoningUsed;

    [DataField]
    public int RendingRunePlacementsAmount = 3;

    /// <summary>
    ///     How close a cultist has to be to a chosen site to draw the rending rune there.
    /// </summary>
    [DataField]
    public float RendingSiteRange = 8f;

    /// <summary>
    ///     Picked at round start from station beacons when the map has no rending markers, so the
    ///     rune is always restricted to a handful of announced places.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public List<RendingSite> RendingSites = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public bool RendingUnlockedAnnounced;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool LeaderSelected;

    /// <summary>
    ///     If no rending rune markers were placed on the map, players will be able to place these runes anywhere on the map
    ///     but no more than <see cref="RendingRunePlacementsAmount">total available</see>.
    /// </summary>
    [DataField]
    public bool EmergencyMarkersMode;

    public int EmergencyMarkersCount;

    /// <summary>
    ///     The entityUid of body which should be sacrificed.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? OfferingTarget;

    /// <summary>
    ///     The target currently written into already granted offering objectives. Kept separately
    ///     so a target changed during objective assignment is reconciled on the next rule tick.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? ObjectivesOfferingTarget;

    /// <summary>
    ///     Set when the offering target is sacrificed on an offering rune.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool OfferingSacrificed;

    /// <summary>
    ///     Departments the offering target is picked from. Leave empty to allow anyone.
    /// </summary>
    [DataField]
    public List<ProtoId<DepartmentPrototype>> OfferingDepartments = new()
    {
        "Security",
        "Command"
    };

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? CultLeader;

    [ViewVariables(VVAccess.ReadOnly)]
    public CultStage Stage = CultStage.Start;

    public CultWinCondition WinCondition = CultWinCondition.Draw;

    #region Veil progression

    [DataField]
    public float VeilRitualCultistRatio;

    [DataField]
    public int VeilRitualMinCultists = 3;

    /// <summary>
    ///     How long after the veil is torn before the blood rift bleeds through.
    /// </summary>
    [DataField]
    public TimeSpan RiftSpawnDelay = TimeSpan.FromMinutes(2);

    [DataField]
    public EntProtoId RiftPrototype = "BloodCultRift";

    /// <summary>
    ///     Recalculated whenever someone tries to start the ritual.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public int MinimumCultistsForVeilRitual = 2;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool VeilWeakened;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? RiftSpawnTime;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Rift;

    /// <summary>
    ///     Human readable location of the rift, already localised.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public string? RiftLocation;

    #endregion

    #region Ascension

    /// <summary>
    ///     Grace period between the cult being told the pentagram is coming and it actually showing up,
    ///     so nobody gets branded mid-conversation with security.
    /// </summary>
    [DataField]
    public TimeSpan RedEyesWarningDelay = TimeSpan.FromMinutes(1);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? RedEyesTime;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool RedEyesApplied;

    [DataField]
    public TimeSpan PentagramWarningDelay = TimeSpan.FromMinutes(2);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? PentagramTime;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool PentagramApplied;

    /// <summary>
    ///     How long the cult gets to be harvesters before the round is called.
    /// </summary>
    [DataField]
    public TimeSpan VictoryEndDelay = TimeSpan.FromSeconds(45);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? VictoryEndTime;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextObjectiveCheck;

    #endregion

    #region Leadership

    /// <summary>
    ///     How long after the round starts before the cult votes on who speaks for Nar'Sie.
    /// </summary>
    [DataField]
    public TimeSpan LeaderVoteDelay = TimeSpan.FromMinutes(5);

    /// <summary>
    ///     Grace period before Nar'Sie calls a new vote after the leader dies.
    /// </summary>
    [DataField]
    public TimeSpan LeaderRevoteDelay = TimeSpan.FromSeconds(45);

    [DataField]
    public TimeSpan LeaderVoteDuration = TimeSpan.FromSeconds(45);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? LeaderVoteTime;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool LeaderVoteRunning;

    #endregion

    public List<Entity<BloodCultistComponent>> Cultists = new();

    public List<Entity<ConstructComponent>> Constructs = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public int PeakCultists;

    [ViewVariables(VVAccess.ReadOnly)]
    public CultStage PeakStage;

    [ViewVariables(VVAccess.ReadOnly)]
    public int TotalConstructs;
}

/// <summary>
///     A place on the station where the veil is thin enough to tear.
/// </summary>
public sealed class RendingSite
{
    public EntityUid Beacon;
    public string Name = string.Empty;
    public bool Used;
}

public enum CultWinCondition : byte
{
    Draw,
    Win,
    Failure
}

public enum CultStage : byte
{
    Start,
    RedEyes,
    Pentagram
}

public sealed class BloodCultNarsieSummoned : EntityEventArgs;
