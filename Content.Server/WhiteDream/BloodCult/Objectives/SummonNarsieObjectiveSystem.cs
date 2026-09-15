// SPDX-License-Identifier: AGPL-3.0-or-later

// WhiteDream - the cult's shared win condition, shown in the character menu next to the sacrifice.
using System.Linq;
using Content.Server.WhiteDream.BloodCult.Gamerule;
using Content.Shared.Objectives.Components;

namespace Content.Server.WhiteDream.BloodCult.Objectives;

[RegisterComponent]
public sealed partial class SummonNarsieObjectiveComponent : Component;

public sealed partial class SummonNarsieObjectiveSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SummonNarsieObjectiveComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<SummonNarsieObjectiveComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        var rule = EntityQuery<BloodCultRuleComponent>().FirstOrDefault();
        args.Progress = rule?.WinCondition == CultWinCondition.Win ? 1f : 0f;
    }
}
