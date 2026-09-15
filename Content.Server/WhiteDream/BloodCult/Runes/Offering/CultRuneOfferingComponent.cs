// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.WhiteDream.BloodCult.Runes.Offering;

[RegisterComponent]
public sealed partial class CultRuneOfferingComponent : Component
{
    /// <summary>
    ///     The lookup range for offering targets
    /// </summary>
    [DataField]
    public float OfferingRange = 0.5f;

    /// <summary>
    ///     The amount of cultists require to convert a living target.
    /// </summary>
    [DataField]
    public int ConvertInvokersAmount = 2;

    /// <summary>
    ///     The amount of cultists required to sacrifice a living target.
    /// </summary>
    [DataField]
    public int AliveSacrificeInvokersAmount = 3;

    /// <summary>
    ///     The amount of charges revive rune system should recieve on sacrifice/convert.
    /// </summary>
    [DataField]
    public int ReviveChargesPerOffering = 1;

    /// <summary>
    ///     WhiteDream - played when the rune takes a life.
    /// </summary>
    [DataField]
    public SoundSpecifier SacrificeSound = new SoundPathSpecifier("/Audio/WhiteDream/BloodCult/curse.ogg");

    /// <summary>
    ///     WhiteDream - played when the rune claims a mind instead.
    /// </summary>
    [DataField]
    public SoundSpecifier ConvertSound = new SoundPathSpecifier("/Audio/WhiteDream/BloodCult/enter_blood.ogg");

    [DataField]
    public EntProtoId SoulShardProto = "SoulShard";

    [DataField]
    public EntProtoId SoulShardGhostProto = "SoulShardGhost";

    [DataField]
    public DamageSpecifier ConvertHealing = new()
    {
        DamageDict = new()
        {
            ["Blunt"] = -40,
            ["Slash"] = -40,
            ["Piercing"] = -40,
            ["Heat"] = -40,
            ["Shock"] = -40,
            ["Cold"] = -40,
            ["Caustic"] = -40
        }
    };
}
