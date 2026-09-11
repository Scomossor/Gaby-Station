// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Throwing;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Network;

namespace Content.Trauma.Shared.Trigger.Triggers;

public sealed partial class TriggerOnLandSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnLandComponent, LandEvent>(OnLand);
    }

    private void OnLand(Entity<TriggerOnLandComponent> ent, ref LandEvent args)
    {
        if (_net.IsClient)
            return;

        _trigger.Trigger(ent.Owner, args.User, ent.Comp.KeyOut);
    }
}
