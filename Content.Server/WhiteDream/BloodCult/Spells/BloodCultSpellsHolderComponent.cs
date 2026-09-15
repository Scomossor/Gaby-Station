// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.WhiteDream.BloodCult.Spells;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.WhiteDream.BloodCult.Spells;

[RegisterComponent]
public sealed partial class BloodCultSpellsHolderComponent : Component
{
    [DataField]
    public int DefaultMaxSpells = 1;

    [DataField]
    public TimeSpan SpellCreationTime = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     WhiteDream - carving a spell into yourself hurts, same as drawing a rune.
    /// </summary>
    [DataField]
    public DamageSpecifier SpellCreationDamage = new() { DamageDict = new() { ["Slash"] = 15 } };

    /// <summary>
    ///     WhiteDream - same audio as drawing a rune: the stab, then the blood.
    /// </summary>
    [DataField]
    public SoundSpecifier SpellCreationStartSound = new SoundPathSpecifier("/Audio/WhiteDream/BloodCult/butcher.ogg");

    [DataField]
    public SoundSpecifier SpellCreationEndSound = new SoundPathSpecifier("/Audio/WhiteDream/BloodCult/blood.ogg");

    [DataField]
    public ProtoId<CultPowerPoolPrototype> PowersPoolPrototype = "BloodCultPowers";

    [DataField]
    public List<EntProtoId> ManagementActions =
    [
        "ActionBloodCultCommune", // Funky - long distance cult telepathy
        "ActionBloodCultStudyVeil", // Funky - progress report
        "ActionBloodCultSelectSpells",
        "ActionBloodCultRemoveSpells"
    ];

    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> SelectedSpells = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> ManagementActionEnts = new();

    public int MaxSpells;

    public DoAfterId? DoAfterId;

    [ViewVariables]
    public EntityUid? TeleportTarget;

    [ViewVariables]
    public TimeSpan TeleportDuration;

}
