// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Trauma.Shared.Genetics;

public sealed partial class GeneticsBloodstreamSystem : EntitySystem
{
    [Dependency] private SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;

    public void SetRefreshAmount(Entity<BloodstreamComponent> ent, FixedPoint2 amount)
        => _bloodstream.TrySetBloodRefreshAmount(ent.AsNullable(), amount);

    public Solution? FlushChemicals(Entity<BloodstreamComponent?> ent, FixedPoint2 quantity)
    {
        if (!Resolve(ent, ref ent.Comp, false)
            || !_solution.ResolveSolution(ent.Owner, ent.Comp.ChemicalSolutionName, ref ent.Comp.ChemicalSolution))
            return null;

        return _solution.SplitSolution(ent.Comp.ChemicalSolution.Value, quantity);
    }

    public bool TryAddToBloodstream(Entity<BloodstreamComponent?> ent, Solution solution)
        => _bloodstream.TryAddToChemicals(ent, solution);
}
