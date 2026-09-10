// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Trauma.Shared.Genetics.Abilities;
using Content.Trauma.Shared.Genetics.Mutations;

namespace Content.Trauma.Server.Genetics.Abilities;

public sealed partial class ThermalRegulatorMutationSystem : EntitySystem
{
    [Dependency] private readonly ThermalRegulatorSystem _regulator = default!;

    [Dependency] private EntityQuery<ThermalRegulatorComponent> _query = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThermalRegulatorMutationComponent, MutationAddedEvent>(OnAdded);
        SubscribeLocalEvent<ThermalRegulatorMutationComponent, MutationRemovedEvent>(OnRemoved);
    }

    private void OnAdded(Entity<ThermalRegulatorMutationComponent> ent, ref MutationAddedEvent args)
    {
        if (!_query.TryComp(args.Target, out var comp))
            return;

        _regulator.ScaleRegulation((args.Target, comp),
            ent.Comp.Shivering,
            ent.Comp.Sweating,
            ent.Comp.Metabolism,
            ent.Comp.Regulation);
    }

    private void OnRemoved(Entity<ThermalRegulatorMutationComponent> ent, ref MutationRemovedEvent args)
    {
        if (!_query.TryComp(args.Target, out var comp))
            return;

        _regulator.ScaleRegulation((args.Target, comp),
            1f / ent.Comp.Shivering,
            1f / ent.Comp.Sweating,
            1f / ent.Comp.Metabolism,
            1f / ent.Comp.Regulation);
    }
}
