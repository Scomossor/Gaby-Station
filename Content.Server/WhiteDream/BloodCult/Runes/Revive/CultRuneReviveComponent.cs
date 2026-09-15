// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.WhiteDream.BloodCult.Runes.Revive;

[RegisterComponent]
public sealed partial class CultRuneReviveComponent : Component
{
    [DataField]
    public float ReviveRange = 0.5f;

    [DataField]
    public DamageSpecifier Healing = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2> // Dumont
        {
            ["Blunt"] = -100,
            ["Slash"] = -100,
            ["Piercing"] = -100,
            ["Heat"] = -100,
            ["Shock"] = -100,
            ["Cold"] = -100,
            ["Caustic"] = -100,
            ["Asphyxiation"] = -100,
            ["Bloodloss"] = -100,
            ["Poison"] = -50,
            ["Cellular"] = -50
        }
    };
}
