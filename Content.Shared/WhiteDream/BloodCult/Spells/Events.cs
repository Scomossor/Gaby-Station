// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Magic;
using Content.Shared.StatusEffect;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.WhiteDream.BloodCult.Spells;

public sealed partial class BloodCultStunEvent : EntityTargetActionEvent, ISpeakSpell
{
    [DataField]
    public TimeSpan ParalyzeDuration = TimeSpan.FromSeconds(16);

    [DataField]
    public TimeSpan MuteDuration = TimeSpan.FromSeconds(12);

    // Dumont changes start
    [DataField]
    public TimeSpan MinParalyzeDuration = TimeSpan.FromSeconds(4);

    [DataField]
    public TimeSpan MinMuteDuration = TimeSpan.FromSeconds(3);

    /// <summary>
    ///     Share of the crew at which the stun has already decayed to its minimum.
    /// </summary>
    [DataField]
    public float DecayShare = 0.4f;

    /// <summary>
    ///     What is left of the stun against someone carrying a mindshield.
    /// </summary>
    [DataField]
    public float MindShieldMultiplier = 0.5f;
    // Dumont end

    [DataField]
    public string? Speech { get; set; }

    public InGameICChatType ChatType => InGameICChatType.Whisper;
}

public sealed partial class BloodCultTeleportEvent : EntityTargetActionEvent, ISpeakSpell
{
    [DataField]
    public float Range = 5;

    [DataField]
    public TimeSpan DoAfterDuration = TimeSpan.FromSeconds(2);

    [DataField]
    public string? Speech { get; set; }

    public InGameICChatType ChatType => InGameICChatType.Whisper;
}

public sealed partial class BloodCultEmpEvent : InstantActionEvent, ISpeakSpell
{
    [DataField]
    public float Range = 4;

    [DataField]
    public float EnergyConsumption = 1000;

    [DataField]
    public float Duration = 20;

    [DataField]
    public string? Speech { get; set; }

    public InGameICChatType ChatType => InGameICChatType.Whisper;
}

public sealed partial class BloodCultShacklesEvent : EntityTargetActionEvent, ISpeakSpell
{
    [DataField]
    public EntProtoId ShacklesProto = "ShadowShackles";

    [DataField]
    public TimeSpan CuffDuration = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan MuteDuration = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan KnockdownDuration = TimeSpan.FromSeconds(1);

    [DataField]
    public string? Speech { get; set; }

    public InGameICChatType ChatType => InGameICChatType.Whisper;
}

public sealed partial class BloodCultTwistedConstructionEvent : EntityTargetActionEvent, ISpeakSpell
{
    [DataField]
    public string? Speech { get; set; }

    public InGameICChatType ChatType => InGameICChatType.Whisper;
}

public sealed partial class SummonEquipmentEvent : InstantActionEvent, ISpeakSpell
{
    /// <summary>
    /// Slot - EntProtoId
    /// </summary>
    [DataField]
    public Dictionary<string, EntProtoId> Prototypes = new();

    [DataField]
    public string? Speech { get; set; }

    public InGameICChatType ChatType => InGameICChatType.Whisper;
}

// Dumont changes start

/// <summary>
///     Drags every living cultist and construct to the leader. One use per cult.
/// </summary>
public sealed partial class BloodCultFinalReckoningEvent : InstantActionEvent, ISpeakSpell
{
    /// <summary>
    ///     How long the leader has to stand still calling the cult in. Long enough that the spell
    ///     cannot be used as an escape button mid-fight.
    /// </summary>
    [DataField]
    public TimeSpan DoAfterDuration = TimeSpan.FromSeconds(10);

    [DataField]
    public string? Speech { get; set; }

    public InGameICChatType ChatType => InGameICChatType.Whisper;
}

/// <summary>
///     Points the whole cult at one of the uninitiated.
/// </summary>
public sealed partial class BloodCultMarkTargetEvent : EntityTargetActionEvent, ISpeakSpell
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(90);

    [DataField]
    public string? Speech { get; set; }

    public InGameICChatType ChatType => InGameICChatType.Whisper;
}

/// <summary>
///     Two clicks: the first takes hold of something, the second throws it across the station.
///     Deliberately a world target and not an entity target. With both, one click on something outside
///     the whitelist invalidates the whole action; with only the world target every click lands and the
///     system picks the entity itself.
/// </summary>
public sealed partial class BloodCultEldritchPulseEvent : WorldTargetActionEvent, ISpeakSpell
{
    [DataField]
    public float Range = 15f;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public string? Speech { get; set; }

    public InGameICChatType ChatType => InGameICChatType.Whisper;
}

// Dumont end

public sealed partial class BloodCultSelectSpellsEvent : InstantActionEvent;

public sealed partial class BloodCultRemoveSpellsEvent : InstantActionEvent;

public sealed partial class BloodSpearRecalledEvent : InstantActionEvent;

/// <summary>
///     WhiteDream - renamed from PlaceTileEntityEvent, which already exists in this codebase.
/// </summary>
public sealed partial class CultPlaceTileEntityEvent : WorldTargetActionEvent
{
    // WhiteDream - renamed from 'Entity', WorldTargetActionEvent declares its own
    [DataField("entity")]
    public EntProtoId? EntityProto;

    [DataField]
    public string? TileId;

    [DataField]
    public SoundSpecifier? Audio;

}

public sealed partial class PhaseShiftEvent : InstantActionEvent
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(5);
}

// Dumont
[Serializable, NetSerializable]
public sealed partial class BloodCultFinalReckoningDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class BloodCultShacklesDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class TwistedConstructionDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class CreateSpeellDoAfterEvent : SimpleDoAfterEvent
{
    public EntProtoId ActionProtoId;
}

[Serializable, NetSerializable]
public sealed partial class TeleportActionDoAfterEvent : SimpleDoAfterEvent
{
    public NetEntity Rune;
    public SoundPathSpecifier TeleportInSound = new("/Audio/WhiteDream/BloodCult/veilin.ogg");
    public SoundPathSpecifier TeleportOutSound = new("/Audio/WhiteDream/BloodCult/veilout.ogg");
}

[Serializable, NetSerializable]
public sealed partial class BloodRitesExtractDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class BloodBeamChargeDoAfterEvent : SimpleDoAfterEvent
{
    public NetCoordinates AimCoordinates;
}

// Dumont
[Serializable, NetSerializable]
public sealed partial class BloodGazeChargeDoAfterEvent : SimpleDoAfterEvent
{
    public NetCoordinates AimCoordinates;
}

[Serializable, NetSerializable]
public enum BloodCultSpellsUiKey : byte
{
    Prepare,
    Remove
}
