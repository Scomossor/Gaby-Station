// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Antag;
using Content.Shared.Mind;
using Content.Shared.StatusIcon;
using Content.Shared._EinsteinEngines.Language;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.WhiteDream.BloodCult.BloodCultist;

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodCultistComponent : Component, IAntagStatusIconComponent
{
    [DataField]
    public float HolyConvertTime = 15f;

    [DataField]
    public int MaximumAllowedEmpowers = 4;

    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "BloodCultMember";

    [DataField]
    public bool IconVisibleToGhost { get; set; } = true;

    [DataField]
    public ProtoId<LanguagePrototype> CultLanguageId { get; set; } = "Eldritch";

    [ViewVariables, NonSerialized]
    public bool WasWeakToHoly;

    [ViewVariables, NonSerialized]
    public EntityUid? BloodSpear;

    [ViewVariables, NonSerialized]
    public Entity<MindComponent>? OriginalMind;

    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 RitesBloodAmount = FixedPoint2.Zero;

    public Color? OriginalEyeColor;

    /// <summary>
    ///     WhiteDream - set once this cultist has been handed their objectives, so we don't retry forever.
    /// </summary>
    [ViewVariables, NonSerialized]
    public bool ObjectivesGranted;

    public CancellationTokenSource? DeconvertToken { get; set; }
}
