// SPDX-License-Identifier: AGPL-3.0-or-later
// Blood Cult: ported from WWhiteDreamProject/wwdpublic. See Content.Shared/WhiteDream/BloodCult/ATTRIBUTION.md

using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.Antag;

public interface IAntagStatusIconComponent
{
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; }

    public bool IconVisibleToGhost { get; set; }
}

