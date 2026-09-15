// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Antag;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.WhiteDream.BloodCult.BloodCultist;

/// <summary>
///     Target marked by the cult leader. The icon is only visible to the cult, its constructs and ghosts.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BloodCultMarkComponent : Component, IAntagStatusIconComponent
{
    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "BloodCultTarget";

    [DataField]
    public bool IconVisibleToGhost { get; set; } = true;

    /// <summary>
    ///     When the mark wears off. Server-side bookkeeping, the icon itself is networked.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan EndTime;
}
