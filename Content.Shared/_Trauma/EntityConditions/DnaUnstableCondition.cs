// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityConditions;

public sealed partial class DnaUnstableCondition : TraumaEntityCondition<DnaUnstableCondition>
{
    public override string GuidebookExplanation(IPrototypeManager prototype)
        => Loc.GetString("entity-condition-guidebook-dna-unstable");
}

public sealed class DnaUnstableConditionSystem : TraumaEntityConditionSystem<MutatableComponent, DnaUnstableCondition>
{
    protected override bool Condition(Entity<MutatableComponent> ent, DnaUnstableCondition condition)
        => ent.Comp.TotalInstability >= ent.Comp.MaxInstability;
}
