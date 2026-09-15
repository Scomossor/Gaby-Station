using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;
using Content.Shared.Body.Prototypes;

namespace Content.Shared.Chemistry.Reagent;

[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class BloodTypeData : ReagentData
{
    /// <summary>
    /// The blood type of the reagent. This is used to determine compatibility.
    /// </summary>
    [DataField]
    public ProtoId<BloodTypePrototype>? Type;

    public BloodTypeData() { }

    public BloodTypeData(BloodTypeData other)
    {
        Type = other.Type;
    }

    public override BloodTypeData Clone()
    {
        return new BloodTypeData(this);
    }

    public override bool Equals(ReagentData? other)
    {
        if ( other == null )
        {
            return false;
        }
        return ((BloodTypeData) other).Type == Type;
    }

    public override int GetHashCode()
    {
        return Type.GetHashCode();
    }
}
