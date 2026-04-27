namespace Content.Shared.Weapons.Ranged.Components;

public abstract partial class BatteryAmmoProviderComponent
{
    [DataField]
    public float ShotsFloat;

    [DataField]
    public float CapacityFloat;
}
