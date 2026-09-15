// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.WhiteDream.BloodCult.Items;

[RegisterComponent, NetworkedComponent]
public sealed partial class CultItemComponent : Component
{
    /// <summary>
    ///     Allow non-cultists to use this item?
    /// </summary>
    [DataField]
    public bool AllowUseToEveryone;

    [DataField]
    public TimeSpan KnockdownDuration = TimeSpan.FromSeconds(2);

    // Dumont changes start
    [DataField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(1);

    [DataField]
    public float BacklashForce = 6000f;

    [DataField]
    public DamageSpecifier BacklashDamage = new() { DamageDict = new() { ["Slash"] = 8 } };
    // Dumont end
}
