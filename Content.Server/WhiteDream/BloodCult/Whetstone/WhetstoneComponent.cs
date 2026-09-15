// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.WhiteDream.BloodCult.Whetstone;

[RegisterComponent]
public sealed partial class WhetstoneComponent : Component
{
    [DataField]
    public int Uses = 1;

    [DataField]
    public DamageSpecifier DamageIncrease = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2> // Dumont
        {
            ["Slash"] = 4
        }
    };

    [DataField]
    public float MaximumIncrease = 25;

    [DataField]
    public EntityWhitelist Whitelist = new();

    [DataField]
    public EntityWhitelist Blacklist = new();

    [DataField]
    public SoundSpecifier SharpenAudio = new SoundPathSpecifier("/Audio/Weapons/bladeslice.ogg");
}
