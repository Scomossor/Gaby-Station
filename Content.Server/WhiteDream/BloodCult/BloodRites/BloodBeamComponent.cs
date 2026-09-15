// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.WhiteDream.BloodCult.BloodRites;

[RegisterComponent]
public sealed partial class BloodBeamComponent : Component
{
    [DataField]
    public TimeSpan ChargeTime = TimeSpan.FromSeconds(3); // Dumont

    [DataField]
    public TimeSpan ShotInterval = TimeSpan.FromSeconds(0.75);

    [DataField]
    public TimeSpan ChargeChantInterval = TimeSpan.FromSeconds(2); // Dumont

    [DataField]
    public int ChargeChantWords = 4; // Dumont

    [DataField]
    public int FireChantWords = 3;

    [DataField]
    public int ShotCount = 12;

    [DataField]
    public float InitialSpread = 40f;

    [DataField]
    public float SpreadStep = 8f;

    [DataField]
    public float ProjectileSpeed = 35f;

    [DataField]
    public EntProtoId ProjectilePrototype = "ProjectileBloodBeam";

    [DataField]
    public SoundSpecifier ChargeSound = new SoundPathSpecifier(
        "/Audio/_Goobstation/Heretic/stargazer/beam_open.ogg"); // Dumont

    [DataField]
    public SoundSpecifier FireSound = new SoundPathSpecifier("/Audio/_Goobstation/Wizard/exit_blood.ogg");

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Charging;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Firing;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? User;

    [ViewVariables(VVAccess.ReadOnly)]
    public Angle AimAngle;

    [ViewVariables(VVAccess.ReadOnly)]
    public int ShotsFired;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextShot;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextChargeChant;
}

[RegisterComponent]
public sealed partial class BloodRiteChantComponent : Component
{
    [DataField]
    public string Incantation = "cult-spell-chant-blood-barrage";

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Spoken;
}

[RegisterComponent]
public sealed partial class BloodBeamProjectileComponent : Component
{
    [DataField]
    public float CultistHealing = 15f;

    [DataField]
    public TimeSpan ParalyzeTime = TimeSpan.FromSeconds(2);

    [DataField]
    public string CultTile = "CultFloor";

    [DataField]
    public EntProtoId TileEffect = "CultTileSpawnEffect";

    [DataField]
    public EntProtoId CultWall = "WallCult";

    [DataField]
    public EntProtoId CultDoor = "CultDoor";

    public readonly HashSet<EntityUid> HitEntities = new();
    public Vector2i? LastTile;
}
