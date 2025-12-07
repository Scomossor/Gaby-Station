using Robust.Shared.GameStates;

namespace Content.Shared._Mono.Emp;

/// <summary>
/// Reduces EMP energy consumption to entity based on coefficient.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class EmpResistanceComponent : Component
{
    [DataField]
    public float Coefficient = 1f;

}
