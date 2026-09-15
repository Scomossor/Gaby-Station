// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Server.WhiteDream.BloodCult.Runes.Apocalypse;

[RegisterComponent]
public sealed partial class CultRuneApocalypseComponent : Component
{
    [DataField]
    public float InvokeTime = 20;

    /// <summary>
    ///     Time between the verses chanted while the rune is being invoked.
    /// </summary>
    [DataField]
    public TimeSpan ChantInterval = TimeSpan.FromSeconds(4);

    /// <summary>
    ///     The opening invocation comes from CultRuneBase; these are the following verses.
    /// </summary>
    [DataField]
    public List<string> ChantPhrases = new()
    {
        "Mah'weyh pleggh at e'ntrath!",
        "N'ath reth sh'yro eth d'rekkathnor!",
        "TOK-LYR RQA-NAP G'OLT-ULOFT!!!",
        "Nar'Sie, k'ah sath ka!",
    };

    [ViewVariables]
    public bool Invoking;

    [ViewVariables]
    public TimeSpan NextChantTime;

    [ViewVariables]
    public int ChantIndex;

    public readonly HashSet<EntityUid> Chanters = new();

    /// <summary>
    ///     If cult has less than this percent of current server population,
    ///     one of the possible events will be triggered.
    /// </summary>
    [DataField]
    public float CultistsThreshold = 0.15f;

    [DataField]
    public float EmpRange = 30f;

    [DataField]
    public float EmpEnergyConsumption = 10000;

    [DataField]
    public float EmpDuration = 180;

    /// <summary>
    ///     Was the rune already used or not.
    /// </summary>
    [DataField]
    public bool Used;

    [DataField]
    public Color UsedColor = Color.DimGray;

    /// <summary>
    ///     These events will be triggered on each rune activation.
    /// </summary>
    [DataField]
    public List<EntProtoId> GuaranteedEvents = new()
    {
        "PowerGridCheck",
        "SolarFlare"
    };

    /// <summary>
    ///     One of these events will be selected on each rune activation.
    ///     Stores the event and how many times it should be repeated.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, int> PossibleEvents = new()
    {
        ["ImmovableRodSpawn"] = 3,
        ["MassHallucinations"] = 2,
        ["KingRatMigration"] = 2,
        ["GameRuleMeteorSwarmMedium"] = 2, // Dumont
        ["SpiderSpawn"] = 3, // Dumont
        ["AnomalySpawn"] = 4,
        ["KudzuGrowth"] = 2,
    };
}
