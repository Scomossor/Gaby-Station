// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Flash;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.Trigger;

public sealed partial class TriggerOnFlashedSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnFlashedComponent, AfterFlashedEvent>(OnFlashed);
    }

    private void OnFlashed(Entity<TriggerOnFlashedComponent> ent, ref AfterFlashedEvent args)
    {
        if (_net.IsClient)
            return;

        if (_random.Prob(ent.Comp.Prob))
            _trigger.Trigger(ent, args.User, ent.Comp.KeyOut);
    }
}
