using Robust.Shared.Prototypes;
using Content.Shared.Damage;

namespace Content.Shared.Body.Prototypes;

[Prototype("bloodtype")]
[DataDefinition]
public sealed partial class BloodTypePrototype : IPrototype
{

    [IdDataField] public string ID { get; private set; } = default!;

    /// <summary>
    /// A list of blood types that are compatible with this blood type.
    /// If a blood type is not in this list, it is considered incompatible.
    /// </summary>
    [DataField("Compat")]
    public List<string>? Compatibilities = new();

    /// <summary>
    /// The damage that will be applied to an entity when it receives foreign blood that
    /// is incompatible with its blood type.
    /// </summary>
    [DataField("DamageList")]
    public DamageSpecifier IncompatibilityDamage = new();

}
