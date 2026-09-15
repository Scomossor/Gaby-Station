// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Mind;
using Content.Server.Roles.Jobs;
using Content.Server.WhiteDream.BloodCult.Gamerule;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;

namespace Content.Server.WhiteDream.BloodCult.Objectives;

public sealed partial class KillTargetCultSystem : EntitySystem
{
    [Dependency] private BloodCultRuleSystem _cultRule = default!;
    [Dependency] private JobSystem _job = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<KillTargetCultComponent, ObjectiveAssignedEvent>(OnObjectiveAssigned);
        SubscribeLocalEvent<KillTargetCultComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<KillTargetCultComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnObjectiveAssigned(Entity<KillTargetCultComponent> ent, ref ObjectiveAssignedEvent args)
    {
        var cultistRule = EntityQuery<BloodCultRuleComponent>().FirstOrDefault();
        if (cultistRule is null)
            return;

        var assignee = args.Mind.OwnedEntity;

        if (cultistRule.OfferingTarget is null
            || assignee == cultistRule.OfferingTarget
            || HasComp<BloodCultistComponent>(cultistRule.OfferingTarget))
        {
            _cultRule.SetRandomCultTarget(cultistRule);
        }

        if (assignee != null && cultistRule.OfferingTarget == assignee)
            cultistRule.OfferingTarget = null;

        // Dumont
        if (cultistRule.OfferingTarget is not { } target || TerminatingOrDeleted(target))
        {
            args.Cancelled = true;
            return;
        }

        ent.Comp.Target = target;
    }

    private void OnAfterAssign(Entity<KillTargetCultComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        if (!ent.Comp.Target.HasValue || !ent.Owner.IsValid() || !HasComp<MetaDataComponent>(ent))
            return;

        _metaData.SetEntityName(ent, GetTitle(ent.Comp.Target.Value, ent.Comp.Title), args.Meta);
    }

    /// <summary>
    ///     Half progress while the target is down, full once it has been offered.
    /// </summary>
    private void OnGetProgress(Entity<KillTargetCultComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        var target = ent.Comp.Target;
        if (!target.HasValue)
        {
            args.Progress = 1f;
            return;
        }

        // Nar'Sie named someone else since this objective was written, so it is no longer this
        // cultist's to finish.
        if (_cultRule.GetTarget() is not { } current || current != target.Value)
        {
            args.Progress = 1f;
            return;
        }

        if (_cultRule.IsObjectiveFinished())
        {
            args.Progress = 1f;
            return;
        }

        args.Progress = !HasComp<MobStateComponent>(target) || _mobState.IsDead(target.Value)
            ? 0.5f
            : 0f;
    }

    private string GetTitle(EntityUid target, string title)
    {
        var targetName = MetaData(target).EntityName;
        var jobName = _mind.TryGetMind(target, out var mindId, out _)
            ? _job.MindTryGetJobName(mindId)
            : Loc.GetString("generic-unknown-title");

        return Loc.GetString(title, ("targetName", targetName), ("job", jobName));
    }
}
