// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Trauma.Shared.Genetics;

public sealed class GeneticsBloodstreamSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public void SetRefreshAmount(Entity<BloodstreamComponent> ent, FixedPoint2 amount)
    {
        ent.Comp.BloodRefreshAmount = amount;
        Dirty(ent);
    }

    public Solution? FlushChemicals(Entity<BloodstreamComponent?> ent, FixedPoint2 quantity)
    {
        if (!Resolve(ent, ref ent.Comp, false)
            || !_solution.ResolveSolution(ent.Owner, ent.Comp.ChemicalSolutionName, ref ent.Comp.ChemicalSolution))
            return null;

        return _solution.SplitSolution(ent.Comp.ChemicalSolution.Value, quantity);
    }

    public bool TryAddToBloodstream(Entity<BloodstreamComponent?> ent, Solution solution)
    {
        if (!Resolve(ent, ref ent.Comp, false)
            || !_solution.ResolveSolution(ent.Owner, ent.Comp.ChemicalSolutionName, ref ent.Comp.ChemicalSolution))
            return false;

        return _solution.TryAddSolution(ent.Comp.ChemicalSolution.Value, solution);
    }
}
