using Content.Shared.Body.Systems;
using Content.Shared.Body.Components;

namespace Content.Server.Body.Systems;

public sealed class BloodTypeSystem : SharedBloodTypeSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BloodTypeComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(Entity<BloodTypeComponent> ent, ref ComponentInit args)
    {
        if (!TryComp(ent.Owner, out BloodTypeComponent? comp))
            return;
        SetBloodType(ent.Owner);
    }
}
