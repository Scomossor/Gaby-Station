using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Body.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class BloodTypeComponent : Component
{
    /// <summary>
    /// The ABO blood type of this entity.
    /// If left null, the type will be define later based on the species of the entity.
    /// Items, such as blood bags, must have a defined blood type in the YAML.
    /// </summary>
    [DataField("ABOType"), AutoNetworkedField]
    public ProtoId<BloodTypePrototype>? Type;

    /// <summary>
    /// The amount of blood that will be deducted from this entity
    /// when damage is applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 ForeignBloodDeducted = 1.0f;


}

