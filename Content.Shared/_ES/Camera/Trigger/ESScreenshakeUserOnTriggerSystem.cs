using Content.Shared.Trigger;

namespace Content.Shared._ES.Camera.Trigger;

public sealed class ESScreenshakeUserOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly ESScreenshakeSystem _screenShake = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESScreenshakeUserOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<ESScreenshakeUserOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        if (args.User == null)
            return;

        _screenShake.Screenshake(args.User.Value, ent.Comp.Translation, ent.Comp.Rotation);
        args.Handled = true;
    }
}
