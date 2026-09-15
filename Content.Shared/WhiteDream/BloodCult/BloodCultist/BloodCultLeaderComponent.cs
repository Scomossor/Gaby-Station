// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Antag;
using Content.Shared.DoAfter;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.WhiteDream.BloodCult.BloodCultist;

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodCultLeaderComponent : Component, IAntagStatusIconComponent
{
    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "BloodCultLeader";

    [DataField]
    public bool IconVisibleToGhost { get; set; } = true;

    // Dumont changes start
    // Do not turn these into a collection expression. The compiler lowers `= [ "a", "b" ]` into
    // CollectionsMarshal.SetCount, which the Robust sandbox rejects, and Content.Shared stops loading.
    [DataField]
    public List<EntProtoId> LeaderActions = new()
    {
        "ActionBloodCultMarkTarget",
        "ActionBloodCultEldritchPulse"
    };

    /// <summary>
    ///     Granted apart from the rest, since the cult only ever gets one of these.
    /// </summary>
    [DataField]
    public EntProtoId FinalReckoningAction = "ActionBloodCultFinalReckoning";

    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> LeaderActionEnts = new();

    /// <summary>
    ///     What the eldritch pulse has hold of, waiting for a destination.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? PulseTarget;

    /// <summary>
    ///     The call currently being made. Kept so it can be cancelled if the leader stops being one.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public DoAfterId? ReckoningDoAfter;

    /// <summary>
    ///     The action entity the call came from, so it can be taken away once it actually lands.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? ReckoningAction;
    // Dumont end
}
