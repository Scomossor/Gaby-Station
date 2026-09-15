// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Text;
using Content.Server.WhiteDream.BloodCult.Commune;
using Content.Server.WhiteDream.BloodCult.RendingRunePlacement;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Content.Shared.WhiteDream.BloodCult.Commune;
using Robust.Shared.Utility;

namespace Content.Server.WhiteDream.BloodCult.Gamerule;

public sealed partial class BloodCultRuleSystem
{
    private static readonly Color VeilColor = new(111, 80, 143, 255);
    private static readonly Color CultColor = new(166, 27, 27, 255);
    private static readonly Color BloodColor = new(139, 0, 0, 255);

    [Dependency] private BloodCultCommuneSystem _commune = default!;

    private void InitializeStatus()
    {
        SubscribeLocalEvent<BloodCultistComponent, BloodCultStudyVeilEvent>(OnStudyVeil);
    }

    private void OnStudyVeil(Entity<BloodCultistComponent> cultist, ref BloodCultStudyVeilEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var rule, out _))
        {
            // Three blocks, three weights - same shape funky uses, so it reads as a report and not a
            // wall of chat.
            _commune.AnnounceToCultist(cultist.Owner, BuildVeilSection(rule), 14, VeilColor);
            _commune.AnnounceToCultist(cultist.Owner, BuildCultSection(rule), 12, CultColor);
            _commune.AnnounceToCultist(cultist.Owner, BuildProgressSection(rule), 11, BloodColor);
            return;
        }
    }

    /// <summary>
    ///     The headline: what the veil is doing right now.
    /// </summary>
    private string BuildVeilSection(BloodCultRuleComponent rule)
    {
        var report = new StringBuilder();
        report.Append("[bold]");
        report.Append(Loc.GetString("cult-status-header"));
        report.Append("[/bold]\n");

        if (!rule.VeilWeakened)
        {
            report.Append(Loc.GetString("cult-status-veil-strong"));
            return report.ToString();
        }

        report.Append(rule.Rift is { } rift && !TerminatingOrDeleted(rift)
            ? Loc.GetString("cult-status-rift-open", ("location", rule.RiftLocation ?? "?"))
            : Loc.GetString("cult-status-rift-pending"));

        return report.ToString();
    }

    /// <summary>
    ///     Who we are.
    /// </summary>
    private string BuildCultSection(BloodCultRuleComponent rule)
    {
        var aliveCultists = rule.Cultists.Count(cultist => !_mobState.IsDead(cultist));
        var aliveConstructs = rule.Constructs.Count(construct => !_mobState.IsDead(construct));

        var report = new StringBuilder();
        report.Append("[bold]");
        report.Append(Loc.GetString("cult-status-cultists", ("count", aliveCultists)));
        report.Append("[/bold]\n");
        report.Append(Loc.GetString("cult-status-constructs", ("count", aliveConstructs)));
        report.Append('\n');

        report.Append(rule.CultLeader is { } leader && !TerminatingOrDeleted(leader)
            ? Loc.GetString("cult-status-leader", ("name", Name(leader)))
            : Loc.GetString("cult-status-leader-none"));

        return report.ToString();
    }

    /// <summary>
    ///     What is left to do.
    /// </summary>
    private string BuildProgressSection(BloodCultRuleComponent rule) => BuildStatusReport(rule);

    /// <summary>
    ///     Builds the full "where are we" report a cultist gets from studying the veil.
    /// </summary>
    private string BuildStatusReport(BloodCultRuleComponent rule)
    {
        var report = new StringBuilder();

        // Stage and what it takes to reach the next one. Stage counts every cultist, dead or not.
        var totalCultists = rule.Cultists.Count;
        report.Append(Loc.GetString("cult-status-stage",
            ("stage", Loc.GetString(GetStageLocId(rule.Stage)))));

        // Dumont
        var eyesRequired = GetRedEyesRequirement(rule);
        var pentagramRequired = GetPentagramRequirement(rule);

        report.Append('\n');
        if (totalCultists < eyesRequired)
        {
            report.Append(Loc.GetString("cult-status-next-eyes",
                ("amount", eyesRequired - totalCultists)));
        }
        else if (totalCultists < pentagramRequired)
        {
            report.Append(Loc.GetString("cult-status-next-pentagram",
                ("amount", pentagramRequired - totalCultists)));
        }
        else
        {
            report.Append(Loc.GetString("cult-status-next-none"));
        }

        // Sacrifice.
        report.Append('\n');
        if (rule.OfferingSacrificed)
        {
            // Dumont
            report.Append(Loc.GetString("cult-status-offering-done"));
        }
        else if (rule.OfferingTarget is { } target && !TerminatingOrDeleted(target))
        {
            report.Append(Loc.GetString(_mobState.IsDead(target)
                    ? "cult-status-offering-dead"
                    : "cult-status-offering",
                ("name", Name(target))));
        }
        else
        {
            report.Append(Loc.GetString("cult-status-offering-none"));
        }

        // Once the veil is torn the rending sites stop mattering - the headline already said where to go.
        if (rule.VeilWeakened)
            return report.ToString();

        // What still stands between us and the rending rune.
        report.Append('\n');
        var required = GetRendingCultistsRequired();
        var missingCultists = required - rule.Cultists.Count;
        var targetDown = IsObjectiveFinished();

        if (missingCultists > 0 || !targetDown)
        {
            report.Append(Loc.GetString("cult-status-rending-locked"));

            if (missingCultists > 0)
            {
                report.Append('\n');
                report.Append(Loc.GetString("cult-status-rending-need-cultists",
                    ("amount", missingCultists),
                    ("required", required)));
            }

            if (!targetDown)
            {
                report.Append('\n');
                report.Append(Loc.GetString("cult-status-rending-need-offering"));
            }

            return report.ToString();
        }

        report.Append(Loc.GetString("cult-status-rending-ready"));
        report.Append('\n');

        // Where the veil is thin.
        if (rule.EmergencyMarkersMode)
        {
            report.Append(Loc.GetString("cult-status-rending-emergency",
                ("amount", rule.EmergencyMarkersCount)));

            return report.ToString();
        }

        report.Append(Loc.GetString("cult-status-rending-header"));

        var found = false;

        foreach (var site in GetAvailableRendingSites(rule))
        {
            found = true;
            report.Append('\n');
            report.Append(Loc.GetString("cult-status-rending-location", ("location", site.Name)));
        }

        var markers = EntityQueryEnumerator<RendingRunePlacementMarkerComponent>();
        while (markers.MoveNext(out var uid, out var marker))
        {
            if (!marker.IsActive)
                continue;

            found = true;
            var location = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(uid));
            report.Append('\n');
            report.Append(Loc.GetString("cult-status-rending-location", ("location", location)));
        }

        if (!found)
        {
            report.Append('\n');
            report.Append(Loc.GetString("cult-status-rending-none"));
        }

        return report.ToString();
    }
}
