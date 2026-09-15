// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Maths.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.WhiteDream.BloodCult.BloodRites;

// Dumont
[RegisterComponent]
public sealed partial class BloodGazeComponent : Component
{
    [DataField]
    public TimeSpan ChargeTime = TimeSpan.FromSeconds(9); // Dumont

    [DataField]
    public TimeSpan ChantInterval = TimeSpan.FromSeconds(3); // Dumont

    [DataField]
    public TimeSpan BloodTrailInterval = TimeSpan.FromSeconds(0.1);

    [DataField]
    public int ChantWords = 3; // Dumont

    [DataField]
    public string BloodReagent = "Blood";

    [DataField]
    public FixedPoint2 BloodPerTile = 20; // Dumont

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(1); // Dumont

    [DataField]
    public EntProtoId ChargeEffect = "EffectBloodGazeOrb"; // Dumont

    [DataField]
    public string CultTile = "CultFloor"; // Dumont

    [DataField]
    public EntProtoId TileEffect = "CultTileSpawnEffect"; // Dumont

    [DataField]
    public EntProtoId CultWall = "WallCult"; // Dumont

    [DataField]
    public EntProtoId CultDoor = "CultDoor"; // Dumont

    [DataField]
    public SoundSpecifier ChargeSound = new SoundPathSpecifier(
        "/Audio/_Goobstation/Wizard/lightning_chargeup.ogg"); // Dumont

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Charging;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Fired;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? User;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextChant;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextBloodTrail;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? ChargeEffectEntity;

    public readonly HashSet<(EntityUid Grid, Vector2i Tile)> BloodiedTiles = new();
    public readonly HashSet<EntityUid> ConvertedStructures = new();
}
