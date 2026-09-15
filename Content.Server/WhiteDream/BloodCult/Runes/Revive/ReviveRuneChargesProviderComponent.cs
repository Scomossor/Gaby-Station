// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server.WhiteDream.BloodCult.Runes.Revive;

[RegisterComponent]
public sealed partial class ReviveRuneChargesProviderComponent : Component
{
    [DataField]
    public int Charges = 3;
}
