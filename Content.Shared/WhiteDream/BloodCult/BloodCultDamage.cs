// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Damage;

namespace Content.Shared.WhiteDream.BloodCult;

public static class BloodCultDamage
{
    /// <summary>
    ///     Copies a ritual blood cost without letting it open wounds.
    /// </summary>
    public static DamageSpecifier WithoutWounds(DamageSpecifier damage)
    {
        var result = new DamageSpecifier(damage);

        // Dumont
        foreach (var (type, amount) in result.DamageDict)
        {
            if (amount > FixedPoint2.Zero)
                result.WoundSeverityMultipliers[type] = FixedPoint2.Zero;
        }

        return result;
    }
}
