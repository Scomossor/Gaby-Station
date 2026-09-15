// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server.WhiteDream.BloodCult.RendingRunePlacement;

[RegisterComponent]
public sealed partial class RendingRunePlacementMarkerComponent : Component
{
    [DataField]
    public bool IsActive;

    [DataField]
    public float DrawingRange = 10;
}
