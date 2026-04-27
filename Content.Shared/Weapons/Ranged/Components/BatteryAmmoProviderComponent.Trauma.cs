namespace Content.Shared.Weapons.Ranged.Components;

public abstract partial class BatteryAmmoProviderComponent
{
    [ViewVariables, AutoNetworkedField]
    public float ShotsFloat;

    [ViewVariables, AutoNetworkedField]
    public float CapacityFloat;
}
