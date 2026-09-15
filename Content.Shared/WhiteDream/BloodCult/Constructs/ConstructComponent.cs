// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Antag;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.WhiteDream.BloodCult.Constructs;

[RegisterComponent]
public sealed partial class ConstructComponent : Component, IAntagStatusIconComponent
{
    [DataField]
    public List<EntProtoId> Actions = new();

    /// <summary>
    ///     Used by the client to determine how long the transform animation should be played.
    /// </summary>
    [DataField]
    public float TransformDelay = 1;

    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "BloodCultMember";

    [DataField]
    public bool IconVisibleToGhost { get; set; } = true;

    #region Shattering

    /// <summary>
    ///     WhiteDream - when this construct dies the body breaks apart instead of lying around, and
    ///     the soul inside falls out as a shard so it can be rebuilt.
    /// </summary>
    [DataField]
    public bool ShattersOnDeath;

    [DataField]
    public EntProtoId ShardProto = "SoulShard";

    [DataField]
    public EntProtoId ShardGhostProto = "SoulShardGhost";

    [DataField]
    public SoundSpecifier ShatterSound = new SoundCollectionSpecifier("GlassBreak");

    /// <summary>
    ///     Cosmetic debris left where the construct fell.
    /// </summary>
    [DataField]
    public EntProtoId? ShatterEffect = "CultTileSpawnEffect";

    #endregion

    public bool Transforming = false;
    public float TransformAccumulator = 0;

    public List<EntityUid?> ActionEntities = new();
}
