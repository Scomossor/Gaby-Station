// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server.WhiteDream.BloodCult.Runes.Empower;

[RegisterComponent]
public sealed partial class CultRuneEmpowerComponent : Component
{
    [DataField]
    public string ComponentToGive = "BloodCultEmpowered";
}
