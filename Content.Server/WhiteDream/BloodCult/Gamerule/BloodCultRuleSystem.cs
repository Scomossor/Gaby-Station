// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Goobstation.Shared.Religion;
using Content.Server.Actions;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.Body.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Hands.Systems;
using Content.Server.Mind;
using Content.Server.NPC.Systems;
using Content.Server.Pinpointer;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Server.RoundEnd;
using Content.Server.WhiteDream.BloodCult.Items.BloodSpear;
using Content.Server.WhiteDream.BloodCult.Objectives;
using Content.Server.WhiteDream.BloodCult.RendingRunePlacement;
using Content.Server.WhiteDream.BloodCult.Spells;
using Content.Server._EinsteinEngines.Language;
using Content.Shared.Antag;
using Content.Shared.Cloning.Events;
using Content.Shared.Cuffs.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Gibbing;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Pinpointer;
using Content.Shared.WhiteDream.BloodCult;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Content.Shared.WhiteDream.BloodCult.Components;
using Content.Shared.WhiteDream.BloodCult.Items;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.WhiteDream.BloodCult.Gamerule;

public sealed partial class BloodCultRuleSystem : GameRuleSystem<BloodCultRuleComponent>
{
    [Dependency] private IRobustRandom _random = default!;

    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private AntagSelectionSystem _antagSelection = default!;
    [Dependency] private BloodSpearSystem _bloodSpear = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private BodySystem _body = default!; // Dumont
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private LanguageSystem _language = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private NpcFactionSystem _faction = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private JobSystem _job = default!;
    [Dependency] private RoleSystem _role = default!;
    [Dependency] private RoundEndSystem _roundEnd = default!;
    [Dependency] private TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultRuleComponent, AfterAntagEntitySelectedEvent>(AfterEntitySelected);

        SubscribeLocalEvent<BloodCultNarsieSummoned>(OnNarsieSummon);

        SubscribeLocalEvent<BloodCultistComponent, ComponentInit>(OnCultistComponentInit);
        SubscribeLocalEvent<BloodCultistComponent, ComponentRemove>(OnCultistComponentRemoved);
        SubscribeLocalEvent<BloodCultistComponent, MobStateChangedEvent>(OnCultistsStateChanged);
        SubscribeLocalEvent<BloodCultistComponent, CloningEvent>(OnClone);

        SubscribeLocalEvent<BloodCultistRoleComponent, GetBriefingEvent>(OnGetBriefing);

        InitializeStatus();
    }

    protected override void Started(
        EntityUid uid,
        BloodCultRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args
    )
    {
        base.Started(uid, component, gameRule, args);

        // Dumont
        component.ProgressionCrewCount = Math.Max(_gameTicker.ReadyPlayerCount(), CountActivePlayers()); // Dumont
        GetRandomRunePlacements(component);

        // WhiteDream - give the cult a few minutes to find each other before they pick a leader.
        ScheduleLeaderVote(component, component.LeaderVoteDelay);
    }

    protected override void AppendRoundEndText(
        EntityUid uid,
        BloodCultRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args
    )
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        // Dumont
        if (component.WinCondition != CultWinCondition.Win)
        {
            component.WinCondition = component.PeakCultists > 0 && CountStandingCultists(component) == 0
                ? CultWinCondition.Failure
                : CultWinCondition.Draw;
        }

        var winText = Loc.GetString($"blood-cult-condition-{component.WinCondition.ToString().ToLower()}");
        args.AddLine(winText);

        // Dumont
        args.AddLine(Loc.GetString("blood-cult-roundend-stats-cultists",
            ("count", Math.Max(component.PeakCultists, component.Cultists.Count))));
        args.AddLine(Loc.GetString("blood-cult-roundend-stats-constructs",
            ("count", Math.Max(component.TotalConstructs, component.Constructs.Count))));
        args.AddLine(Loc.GetString("blood-cult-roundend-stats-stage",
            ("stage", Loc.GetString(GetStageLocId(component.PeakStage)))));

        args.AddLine(Loc.GetString("blood-cultists-list-start"));

        var sessionData = _antagSelection.GetAntagIdentifiers(uid);
        foreach (var (_, data, name) in sessionData)
        {
            var lising = Loc.GetString("blood-cultists-list-name", ("name", name), ("user", data.UserName));
            args.AddLine(lising);
        }
    }

    #region EventHandlers

    private void AfterEntitySelected(Entity<BloodCultRuleComponent> ent, ref AfterAntagEntitySelectedEvent args) =>
        MakeCultist(args.EntityUid, ent);

    private void OnNarsieSummon(BloodCultNarsieSummoned ev)
    {
        var rulesQuery = QueryActiveRules();
        while (rulesQuery.MoveNext(out _, out var cult, out _))
        {
            cult.WinCondition = CultWinCondition.Win;

            // <WhiteDream>
            // Query the world instead of cult.Cultists: gibbing mutates that list as we go, which used
            // to throw halfway through and leave everyone but the first cultist as a ghost.
            var cultists = new List<EntityUid>();
            var cultistQuery = EntityQueryEnumerator<BloodCultistComponent>();
            while (cultistQuery.MoveNext(out var cultistUid, out _))
                cultists.Add(cultistUid);

            foreach (var cultist in cultists)
            {
                if (TerminatingOrDeleted(cultist) || !_mind.TryGetMind(cultist, out var mindId, out _))
                    continue;

                var harvester = Spawn(cult.HarvesterPrototype, Transform(cultist).Coordinates);
                _mind.TransferTo(mindId, harvester);
                _language.UpdateEntityLanguages(harvester);
                _body.GibBody(cultist); // Dumont
            }

            // Let them actually be harvesters for a bit before the round is called.
            cult.VictoryEndTime = _timing.CurTime + cult.VictoryEndDelay;
            // </WhiteDream>
            return;
        }
    }

    private void OnCultistComponentInit(Entity<BloodCultistComponent> cultist, ref ComponentInit args)
    {
        _language.AddLanguage(cultist.Owner, cultist.Comp.CultLanguageId);

        // Dumont changes start
        if (HasComp<WeakToHolyComponent>(cultist))
            cultist.Comp.WasWeakToHoly = true;
        else
            EnsureComp<WeakToHolyComponent>(cultist).AlwaysTakeHoly = true;
        // Dumont end

        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var cult, out _))
        {
            cult.Cultists.Add(cultist);
            cult.PeakCultists = Math.Max(cult.PeakCultists, cult.Cultists.Count);
            UpdateCultStage(cult);

            // WhiteDream - anyone converted after the cult already reached a stage still gets its marks.
            ApplyCurrentStageAppearance(cult, cultist);
        }
    }

    /// <summary>
    ///     Brings a single cultist up to date with the stage the cult is already at.
    /// </summary>
    private void ApplyCurrentStageAppearance(BloodCultRuleComponent cultRule, Entity<BloodCultistComponent> cultist)
    {
        // Dumont
        if (cultRule.RedEyesApplied)
        {
            cultist.Comp.OriginalEyeColor ??= GetCultistEyeColor(cultist); // Dumont
            SetCultistEyeColor(cultist, cultRule.EyeColor); // Dumont
        }

        if (cultRule.PentagramApplied)
            EnsureComp<PentagramComponent>(cultist);
    }

    private void OnCultistComponentRemoved(Entity<BloodCultistComponent> cultist, ref ComponentRemove args)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var cult, out _))
            cult.Cultists.Remove(cultist);

        CheckWinCondition();

        if (TerminatingOrDeleted(cultist.Owner))
            return;

        // Dumont
        if (!cultist.Comp.WasWeakToHoly)
            RemComp<WeakToHolyComponent>(cultist);

        RemoveAllCultItems(cultist);
        RemoveCultistAppearance(cultist);
        RemoveObjectiveAndRole(cultist.Owner);
        _language.RemoveLanguage(cultist.Owner, cultist.Comp.CultLanguageId);

        if (!TryComp(cultist, out BloodCultSpellsHolderComponent? powersHolder))
            return;

        foreach (var power in powersHolder.SelectedSpells)
            _actions.RemoveAction(cultist.Owner, power);
    }

    private void OnCultistsStateChanged(Entity<BloodCultistComponent> cultist, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        CheckWinCondition();
        CheckLeaderAlive(cultist); // WhiteDream - Nar'Sie calls a new vote if her voice fell.
    }

    private void OnClone(Entity<BloodCultistComponent> cultist, ref CloningEvent args) =>
        RemoveObjectiveAndRole(cultist);

    private void OnGetBriefing(Entity<BloodCultistRoleComponent> cultist, ref GetBriefingEvent args)
    {
        args.Append(Loc.GetString("blood-cult-role-briefing-short"));
        var rulesQuery = QueryActiveRules();
        while (rulesQuery.MoveNext(out _, out var rule, out _))
        {
            if (!rule.EmergencyMarkersMode)
                continue;

            args.Append(
                Loc.GetString("blood-cult-role-briefing-emergency-rending", ("amount", rule.EmergencyMarkersCount)));
            return;
        }

        // WhiteDream - beacon-picked sites go in the briefing too.
        var siteQuery = QueryActiveRules();
        while (siteQuery.MoveNext(out _, out var siteRule, out _))
        {
            foreach (var site in GetAvailableRendingSites(siteRule))
                args.Append(Loc.GetString("blood-cult-role-briefing-rending-site", ("location", site.Name)));
        }

        var query = EntityQueryEnumerator<RendingRunePlacementMarkerComponent>();
        while (query.MoveNext(out var uid, out var marker))
        {
            if (!marker.IsActive)
                continue;

            var navMapLocation = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(uid));
            var coordinates = Transform(uid).Coordinates;
            var msg = Loc.GetString(
                "blood-cult-role-briefing-rending-locations",
                ("location", navMapLocation),
                ("coordinates", coordinates.Position));
            args.Append(msg); // WhiteDream - msg is already localised
        }
    }

    #endregion

    // Dumont
    private static readonly EntProtoId DefaultRule = "BloodCult";

    public void Convert(EntityUid target)
    {
        if (!TryComp(target, out ActorComponent? actor))
            return;

        // <WhiteDream>
        _antagSelection.ForceMakeAntag<BloodCultRuleComponent>(actor.PlayerSession, DefaultRule);
        // </WhiteDream>
    }

    /// <summary>
    ///     Whether the offering target was already given up on a rune.
    /// </summary>
    public bool IsObjectiveFinished()
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var rule, out _))
            return rule.OfferingSacrificed;

        return false;
    }

    /// <summary>
    ///     Called by the offering rune when the marked one is actually consumed on it.
    /// </summary>
    public void MarkOfferingSacrificed(EntityUid target)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var rule, out _))
        {
            if (rule.OfferingTarget != target || rule.OfferingSacrificed)
                continue;

            rule.OfferingSacrificed = true;
            NotifyCultists(Loc.GetString("cult-offering-accepted", ("name", Name(target))));
        }
    }

    public bool TryGetTarget([NotNullWhen(true)] out EntityUid? target)
    {
        target = GetTarget();
        return target is not null;
    }

    public EntityUid? GetTarget()
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var bloodCultRule, out _))
            if (bloodCultRule.OfferingTarget.HasValue)
                return bloodCultRule.OfferingTarget.Value;

        return null;
    }

    public bool IsTarget(EntityUid entityUid)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var rule, out _))
            return entityUid == rule.OfferingTarget;

        return false;
    }

    /// <summary>
    ///     Crew count captured at round start, used by the percentage based requirements.
    /// </summary>
    public int GetProgressionCrewCount()
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var rule, out _))
            return rule.ProgressionCrewCount;

        return 0;
    }

    public int GetRedEyesRequirement(BloodCultRuleComponent cultRule)
    {
        return (int) MathF.Ceiling(cultRule.ProgressionCrewCount * cultRule.ReadEyeThreshold);
    }

    public int GetPentagramRequirement(BloodCultRuleComponent cultRule)
    {
        return (int) MathF.Ceiling(cultRule.ProgressionCrewCount * cultRule.PentagramThreshold);
    }

    public int GetTotalCultists()
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var rule, out _))
            // Conversion thresholds describe the share of the crew that joined the cult.
            // Constructs are tracked separately and must not make the rending rune appear early.
            return rule.Cultists.Count;

        return 0;
    }

    public void RemoveObjectiveAndRole(EntityUid uid)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind))
            return;

        var objectives = mind.Objectives.FindAll(HasComp<KillTargetCultComponent>);
        foreach (var obj in objectives)
            _mind.TryRemoveObjective(mindId, mind, mind.Objectives.IndexOf(obj));

        if (_role.MindHasRole<BloodCultistRoleComponent>(mindId))
            _role.MindRemoveRole<BloodCultistRoleComponent>(mindId);
    }

    public bool CanDrawRendingRune(EntityUid user)
    {
        var ruleQuery = QueryActiveRules();
        while (ruleQuery.MoveNext(out _, out var rule, out _))
        {
            if (rule is { EmergencyMarkersMode: true, EmergencyMarkersCount: > 0 })
                return true;

            // WhiteDream - beacon-picked sites. Checking only, the site is spent on activation.
            if (IsNearRendingSite(rule, user, out _))
                return true;
        }

        var query = EntityQueryEnumerator<RendingRunePlacementMarkerComponent>();
        while (query.MoveNext(out var uid, out var marker))
        {
            if (!marker.IsActive)
                continue;

            var userLocation = Transform(user).Coordinates;
            var placementCoordinates = Transform(uid).Coordinates;
            if (_transform.InRange(placementCoordinates, userLocation, marker.DrawingRange))
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Picks the offering target, preferring the departments listed on the rule.
    /// </summary>
    public void SetRandomCultTarget(BloodCultRuleComponent rule)
    {
        var query =
            EntityQueryEnumerator<MindContainerComponent, HumanoidAppearanceComponent, ActorComponent>();

        var wanted = new List<EntityUid>();
        var everyoneElse = new List<EntityUid>();

        while (query.MoveNext(out var uid, out var mindContainer, out _, out _))
        {
            if (!IsAvailableOfferingTarget(uid))
                continue;

            if (IsWorthOffering(rule, mindContainer))
                wanted.Add(uid);
            else
                everyoneElse.Add(uid);
        }

        var pool = wanted.Count > 0 ? wanted : everyoneElse;
        rule.OfferingTarget = pool.Count > 0 ? _random.Pick(pool) : null;
    }

    /// <summary>
    ///     Whether this person holds a job in one of the departments the offering is restricted to.
    /// </summary>
    private bool IsWorthOffering(BloodCultRuleComponent rule, MindContainerComponent mindContainer)
    {
        // No restriction configured, so anyone will do and nobody is preferred.
        if (rule.OfferingDepartments.Count == 0)
            return false;

        if (mindContainer.Mind is not { } mind)
            return false;

        if (!_job.MindTryGetJob(mind, out var job) ||
            !_job.TryGetAllDepartments(job.ID, out var departments))
            return false;

        return departments.Any(department => rule.OfferingDepartments.Contains(department.ID));
    }

    public bool TryConsumeNearestMarker(EntityUid user)
    {
        var ruleQuery = QueryActiveRules();
        while (ruleQuery.MoveNext(out _, out var rule, out _))
        {
            if (rule is { EmergencyMarkersMode: true, EmergencyMarkersCount: > 0 })
            {
                rule.EmergencyMarkersCount--;
                return true;
            }

            // WhiteDream - spend the beacon site the cultist is standing at.
            if (IsNearRendingSite(rule, user, out var site) && site is not null)
            {
                site.Used = true;
                return true;
            }
        }

        var userLocation = Transform(user).Coordinates;
        var query = EntityQueryEnumerator<RendingRunePlacementMarkerComponent>();
        while (query.MoveNext(out var markerUid, out var marker))
        {
            if (!marker.IsActive)
                continue;

            var placementCoordinates = Transform(markerUid).Coordinates;
            if (!_transform.InRange(placementCoordinates, userLocation, marker.DrawingRange))
                continue;

            marker.IsActive = false;
            return true;
        }

        return false;
    }

    private void CheckWinCondition()
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var cult, out _))
        {
            // Dumont
            if (cult.WinCondition == CultWinCondition.Win)
                continue;

            if (cult.PeakCultists == 0 || CountStandingCultists(cult) != 0)
                continue;

            cult.WinCondition = CultWinCondition.Failure;
        }
    }

    private int CountStandingCultists(BloodCultRuleComponent cult)
    {
        return cult.Cultists.Count(cultist =>
            !TerminatingOrDeleted(cultist.Owner) &&
            HasComp<MobStateComponent>(cultist.Owner) &&
            !_mobState.IsDead(cultist));
    }

    private void MakeCultist(EntityUid cultist, Entity<BloodCultRuleComponent> rule)
    {
        if (!_mind.TryGetMind(cultist, out var mindId, out var mind))
            return;

        EnsureComp<BloodCultSpellsHolderComponent>(cultist);

        _faction.RemoveFaction(cultist, rule.Comp.NanoTrasenFaction);
        _faction.AddFaction(cultist, rule.Comp.BloodCultFaction);

        if (rule.Comp.OfferingTarget is null)
            SetRandomCultTarget(rule.Comp);

        if (rule.Comp.OfferingTarget is { } target && target != cultist)
            _mind.TryAddObjective(mindId, mind, "KillTargetCultObjective");

        // WhiteDream - the round start popup was removed: the antag briefing (chat + character menu)
        // already carries the same text, and the Study the Veil action gives live status on demand.
    }

    private static string GetStageLocId(CultStage stage) => stage switch
    {
        CultStage.RedEyes => "blood-cult-stage-red-eyes",
        CultStage.Pentagram => "blood-cult-stage-pentagram",
        _ => "blood-cult-stage-start",
    };

    private void GetRandomRunePlacements(BloodCultRuleComponent component)
    {
        var allMarkers = EntityQuery<RendingRunePlacementMarkerComponent>().ToList();
        if (allMarkers.Count == 0)
        {
            // WhiteDream - no mapper placed markers, so pick a few station beacons instead. The rune
            // stays restricted to a handful of named places rather than "anywhere".
            PickRendingSitesFromBeacons(component);
            return;
        }

        var maxRunes = component.RendingRunePlacementsAmount;
        if (allMarkers.Count < component.RendingRunePlacementsAmount)
            maxRunes = allMarkers.Count;

        for (var i = maxRunes; i > 0; i--)
        {
            var marker = _random.PickAndTake(allMarkers);
            marker.IsActive = true;
        }
    }

    /// <summary>
    ///     Chooses the places where the veil is thin from the station's own beacons.
    /// </summary>
    private void PickRendingSitesFromBeacons(BloodCultRuleComponent component)
    {
        var beacons = new List<EntityUid>();
        var query = EntityQueryEnumerator<NavMapBeaconComponent>();
        while (query.MoveNext(out var uid, out var beacon))
        {
            // WhiteDream - only beacons that belong to a station. Otherwise the veil ends up thin
            // on the escape shuttle, on debris, or on some off-station ruin.
            if (beacon.Enabled && _station.GetOwningStation(uid) is not null)
                beacons.Add(uid);
        }

        if (beacons.Count == 0)
        {
            // Truly nothing to anchor to. Fall back to the old free-for-all so the round isn't stuck.
            component.EmergencyMarkersMode = true;
            component.EmergencyMarkersCount = component.RendingRunePlacementsAmount;
            return;
        }

        _random.Shuffle(beacons);
        var amount = Math.Min(component.RendingRunePlacementsAmount, beacons.Count);

        for (var i = 0; i < amount; i++)
        {
            var beacon = beacons[i];
            component.RendingSites.Add(new RendingSite
            {
                Beacon = beacon,
                Name = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(beacon)),
            });
        }
    }

    /// <summary>
    ///     Every site the cult can still tear open.
    /// </summary>
    public IEnumerable<RendingSite> GetAvailableRendingSites(BloodCultRuleComponent component)
    {
        return component.RendingSites.Where(site => !site.Used && !TerminatingOrDeleted(site.Beacon));
    }

    private bool IsNearRendingSite(BloodCultRuleComponent component, EntityUid user, out RendingSite? found)
    {
        found = null;
        var userLocation = Transform(user).Coordinates;

        foreach (var site in GetAvailableRendingSites(component))
        {
            if (!_transform.InRange(Transform(site.Beacon).Coordinates, userLocation, component.RendingSiteRange))
                continue;

            found = site;
            return true;
        }

        return false;
    }

    private void RemoveAllCultItems(Entity<BloodCultistComponent> cultist)
    {
        if (!_inventory.TryGetContainerSlotEnumerator(cultist.Owner, out var enumerator))
            return;

        _bloodSpear.DetachSpearFromMaster(cultist);
        while (enumerator.MoveNext(out var container))
            if (container.ContainedEntity != null && HasComp<CultItemComponent>(container.ContainedEntity.Value))
                _container.Remove(container.ContainedEntity.Value, container, true, true);

        foreach (var item in _hands.EnumerateHeld((cultist.Owner, null)))
            if (TryComp(item, out CultItemComponent? cultItem) && !cultItem.AllowUseToEveryone &&
                !_hands.TryDrop(cultist.Owner, item, null, false, false))
                QueueDel(item);
    }

    private void RemoveCultistAppearance(Entity<BloodCultistComponent> cultist)
    {
        if (cultist.Comp.OriginalEyeColor is { } originalEyeColor)
            SetCultistEyeColor(cultist, originalEyeColor); // Dumont

        RemComp<PentagramComponent>(cultist);
    }

    private void UpdateCultStage(BloodCultRuleComponent cultRule)
    {
        var cultistsCount = cultRule.Cultists.Count;
        var prevStage = cultRule.Stage;

        // Dumont
        if (cultistsCount >= GetPentagramRequirement(cultRule))
            cultRule.Stage = CultStage.Pentagram;
        else if (cultistsCount >= GetRedEyesRequirement(cultRule))
            cultRule.Stage = CultStage.RedEyes;
        else
            cultRule.Stage = CultStage.Start;

        // Dumont
        if (cultRule.Stage > cultRule.PeakStage)
            cultRule.PeakStage = cultRule.Stage;

        if (cultRule.Stage != prevStage)
            UpdateCultistsAppearance(cultRule, prevStage);
    }

    private void UpdateCultistsAppearance(BloodCultRuleComponent cultRule, CultStage prevStage)
    {
        // Dumont
        switch (cultRule.Stage)
        {
            case CultStage.Start when prevStage == CultStage.RedEyes:
                cultRule.RedEyesTime = null;
                break;
            case CultStage.RedEyes when prevStage == CultStage.Start:
                BeginRedEyes(cultRule);
                break;
            case CultStage.RedEyes when prevStage == CultStage.Pentagram:
                cultRule.PentagramTime = null;
                break;
            case CultStage.Pentagram:
                // A mass conversion can move the cult straight from Start to Pentagram without ever
                // passing through RedEyes. Start both: BeginRedEyes no-ops if
                // it already ran, so nobody skips the eyes just by growing fast.
                BeginRedEyes(cultRule);

                // WhiteDream - warn the cult first, brand them two minutes later.
                BeginAscension(cultRule);
                break;
        }
    }

    // Dumont

    // Dumont changes start
    private int CountActivePlayers()
    {
        var count = 0;
        var query = EntityQueryEnumerator<MindContainerComponent, HumanoidAppearanceComponent, ActorComponent>();
        while (query.MoveNext(out _, out _, out _, out _))
            count++;

        return count;
    }

    private Color? GetCultistEyeColor(EntityUid cultist)
    {
        return TryComp(cultist, out HumanoidAppearanceComponent? appearance) ? appearance.EyeColor : null;
    }

    private void SetCultistEyeColor(EntityUid cultist, Color color)
    {
        if (!TryComp(cultist, out HumanoidAppearanceComponent? appearance))
            return;

        appearance.EyeColor = color;
        Dirty(cultist, appearance);
    }
    // Dumont end

}
