// SPDX-License-Identifier: AGPL-3.0-or-later

// WhiteDream - the cult picks who speaks for Nar'Sie, instead of her picking at random.
using System.Linq;
using Content.Server.Voting;
using Content.Server.Voting.Managers;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Robust.Shared.Player;

namespace Content.Server.WhiteDream.BloodCult.Gamerule;

public sealed partial class BloodCultRuleSystem
{
    [Dependency] private IVoteManager _voteManager = default!;

    private void TickLeaderVote(BloodCultRuleComponent rule)
    {
        if (rule.LeaderVoteRunning || rule.LeaderVoteTime is not { } time || _timing.CurTime < time)
            return;

        rule.LeaderVoteTime = null;
        StartLeaderVote(rule);
    }

    /// <summary>
    ///     Schedules a vote, unless one is already coming or running.
    /// </summary>
    public void ScheduleLeaderVote(BloodCultRuleComponent rule, TimeSpan delay)
    {
        if (rule.LeaderVoteRunning || rule.LeaderVoteTime is not null)
            return;

        rule.LeaderVoteTime = _timing.CurTime + delay;
    }

    private List<EntityUid> GetLeaderCandidates(BloodCultRuleComponent rule)
    {
        return rule.Cultists
            .Where(cultist => !TerminatingOrDeleted(cultist.Owner)
                              && !_mobState.IsDead(cultist.Owner)
                              && HasComp<ActorComponent>(cultist.Owner))
            .Select(cultist => cultist.Owner)
            .ToList();
    }

    private void StartLeaderVote(BloodCultRuleComponent rule)
    {
        var candidates = GetLeaderCandidates(rule);
        if (candidates.Count == 0)
            return;

        // Nobody to choose between - just crown them.
        if (candidates.Count == 1)
        {
            PromoteLeader(rule, candidates[0]);
            return;
        }

        var options = new VoteOptions
        {
            Title = Loc.GetString("cult-leader-vote-title"),
            InitiatorText = Loc.GetString("cult-leader-vote-initiator"),
            Duration = rule.LeaderVoteDuration,
            VoterEligibility = VoteManager.VoterEligibility.BloodCult,
        };

        foreach (var candidate in candidates)
            options.Options.Add((Name(candidate), candidate));

        rule.LeaderVoteRunning = true;
        NotifyCultists(Loc.GetString("cult-leader-vote-started"));

        var vote = _voteManager.CreateVote(options);
        vote.OnFinished += (_, args) =>
        {
            rule.LeaderVoteRunning = false;

            // On a stalemate just take the first of the tied candidates.
            var winner = args.Winner as EntityUid?
                         ?? (args.Winners.Length > 0 ? args.Winners[0] as EntityUid? : null);

            if (winner is not { } chosen || TerminatingOrDeleted(chosen) || _mobState.IsDead(chosen))
            {
                ScheduleLeaderVote(rule, rule.LeaderRevoteDelay);
                return;
            }

            PromoteLeader(rule, chosen);
        };

        vote.OnCancelled += _ =>
        {
            rule.LeaderVoteRunning = false;
            ScheduleLeaderVote(rule, rule.LeaderRevoteDelay);
        };
    }

    private void PromoteLeader(BloodCultRuleComponent rule, EntityUid leader)
    {
        if (rule.CultLeader is { } previous && previous != leader && !TerminatingOrDeleted(previous))
            RemComp<BloodCultLeaderComponent>(previous);

        // Dumont
        rule.CultLeader = leader;
        rule.LeaderSelected = true;

        EnsureComp<BloodCultLeaderComponent>(leader);

        NotifyCultists(Loc.GetString("cult-leader-chosen", ("name", Name(leader))));
    }

    /// <summary>
    ///     Called when a cultist dies. If it was the leader, Nar'Sie calls for another.
    /// </summary>
    private void CheckLeaderAlive(EntityUid dead)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var rule, out _))
        {
            if (rule.CultLeader == dead)
                LoseLeader(rule, dead);
        }
    }

    private void LoseLeader(BloodCultRuleComponent rule, EntityUid leader)
    {
        if (!TerminatingOrDeleted(leader))
            RemCompDeferred<BloodCultLeaderComponent>(leader);

        rule.CultLeader = null;
        rule.LeaderSelected = false;

        NotifyCultists(Loc.GetString("cult-leader-lost"));
        ScheduleLeaderVote(rule, rule.LeaderRevoteDelay);
    }

    private void ReconcileLeader(BloodCultRuleComponent rule)
    {
        // Nar'Sie is already through and the cult is being harvested.
        if (rule.VictoryEndTime is not null || rule.WinCondition == CultWinCondition.Win)
            return;

        var orphaned = false;

        var stray = EntityQueryEnumerator<BloodCultLeaderComponent>();
        while (stray.MoveNext(out var uid, out _))
        {
            if (rule.CultLeader == uid)
                continue;

            RemCompDeferred<BloodCultLeaderComponent>(uid);
            orphaned = true;
        }

        if (rule.CultLeader is { } leader)
        {
            if (TerminatingOrDeleted(leader) || _mobState.IsDead(leader) || !HasComp<BloodCultistComponent>(leader))
                LoseLeader(rule, leader);

            return;
        }

        // Someone was wearing the mark without the rule knowing. Now that it is off them, get a
        // real leader elected instead of waiting for a death that will never be noticed.
        if (orphaned)
        {
            rule.LeaderSelected = false;
            ScheduleLeaderVote(rule, rule.LeaderRevoteDelay);
        }
    }
}
