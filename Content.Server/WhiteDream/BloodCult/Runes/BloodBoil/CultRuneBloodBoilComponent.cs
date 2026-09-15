// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.WhiteDream.BloodCult.Runes.BloodBoil;

[RegisterComponent]
public sealed partial class CultRuneBloodBoilComponent : Component
{
    [DataField]
    public EntProtoId ProjectilePrototype = "BloodBoilProjectile";

    [DataField]
    public float ProjectileSpeed = 15; // WhiteDream - 50 was so fast the projectile was invisible

    [DataField]
    public float TargetsLookupRange = 15f;

    [DataField]
    public int ProjectileCount = 3;

    [DataField]
    public float FireStacksPerProjectile = 1;

    [DataField]
    public SoundSpecifier ActivationSound = new SoundPathSpecifier("/Audio/WhiteDream/BloodCult/magic.ogg");
}
