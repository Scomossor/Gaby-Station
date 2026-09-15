// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.Interaction;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Events;

namespace Content.Shared.WhiteDream.BloodCult.Repulsor;

public sealed partial class RepulseSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedStunSystem _stunSystem = default!;
    [Dependency] private ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RepulseOnTouchComponent, StartCollideEvent>(HandleCollision);
        SubscribeLocalEvent<RepulseComponent, InteractHandEvent>(OnHandInteract);
    }

    private void HandleCollision(Entity<RepulseOnTouchComponent> touchRepulsor, ref StartCollideEvent args)
    {
        if (!TryComp(touchRepulsor, out RepulseComponent? repulse))
            return;

        Repulse((touchRepulsor.Owner, repulse), args.OtherEntity);
    }

    private void OnHandInteract(Entity<RepulseComponent> repulsor, ref InteractHandEvent args)
    {
        Repulse(repulsor, args.User);
    }

    public void Repulse(Entity<RepulseComponent> repulsor, EntityUid user)
    {
        var ev = new BeforeRepulseEvent(user);
        RaiseLocalEvent(repulsor, ev);
        if (ev.Cancelled)
            return;

        var direction = _transform.GetMapCoordinates(user).Position - _transform.GetMapCoordinates(repulsor).Position;
        if (direction.LengthSquared() > 0f)
            direction = direction.Normalized();
        else
            direction = new Vector2(0f, -1f);

        _throwing.TryThrow(user,
            direction * repulsor.Comp.Distance,
            repulsor.Comp.Speed,
            recoil: false,
            doSpin: false,
            playSound: false);

        _stunSystem.TryAddStunDuration(user, repulsor.Comp.StunDuration);
        _stunSystem.TryKnockdown(user, repulsor.Comp.KnockdownDuration, force: true);
    }
}
