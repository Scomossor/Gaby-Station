// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Shared.Religion;
using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
using Content.Server.Pinpointer;
using Content.Server.Popups;
using Content.Server.WhiteDream.BloodCult.Gamerule;
using Content.Shared.Actions.Components;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Content.Shared.WhiteDream.BloodCult.Constructs;
using Content.Shared.WhiteDream.BloodCult.Spells;
using Content.Shared.Whitelist;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.WhiteDream.BloodCult.Spells;

/// <summary>
///     Spells granted only while the entity carries <see cref="BloodCultLeaderComponent"/>.
/// </summary>
public sealed partial class BloodCultLeaderSpellsSystem : EntitySystem
{
    private static readonly EntProtoId TeleportInEffect = "CultTeleportInEffect";
    private static readonly EntProtoId TeleportOutEffect = "CultTeleportOutEffect";

    /// <summary>
    ///     How close to the clicked tile the pulse looks for something to grab.
    /// </summary>
    private const float PulsePickRange = 0.4f;

    private static readonly TimeSpan ActionCheckInterval = TimeSpan.FromSeconds(5);

    private TimeSpan _nextActionCheck;

    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private BloodCultRuleSystem _cultRule = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private DivineInterventionSystem _divineIntervention = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private DoAfterSystem _doAfter = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultLeaderComponent, ComponentStartup>(OnLeaderStartup);
        SubscribeLocalEvent<BloodCultLeaderComponent, ComponentShutdown>(OnLeaderShutdown);

        SubscribeLocalEvent<BloodCultLeaderComponent, BloodCultFinalReckoningEvent>(OnFinalReckoning);
        SubscribeLocalEvent<BloodCultLeaderComponent, BloodCultFinalReckoningDoAfterEvent>(OnFinalReckoningDoAfter);
        SubscribeLocalEvent<BloodCultLeaderComponent, BloodCultMarkTargetEvent>(OnMarkTarget);
        SubscribeLocalEvent<BloodCultLeaderComponent, BloodCultEldritchPulseEvent>(OnEldritchPulse);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<BloodCultMarkComponent>();
        while (query.MoveNext(out var uid, out var mark))
        {
            if (now < mark.EndTime)
                continue;

            RemCompDeferred<BloodCultMarkComponent>(uid);
        }

        if (now < _nextActionCheck)
            return;

        _nextActionCheck = now + ActionCheckInterval;

        // Dumont
        var leaders = EntityQueryEnumerator<BloodCultLeaderComponent>();
        while (leaders.MoveNext(out var uid, out var comp))
            EnsureLeaderActions((uid, comp));
    }

    #region Granting

    private void OnLeaderStartup(Entity<BloodCultLeaderComponent> leader, ref ComponentStartup args) =>
        EnsureLeaderActions(leader);

    /// <summary>
    ///     Gives the leader anything they are owed and nothing they already hold. Safe to call
    ///     repeatedly, which is the point: it runs on promotion and again on a timer.
    /// </summary>
    private void EnsureLeaderActions(Entity<BloodCultLeaderComponent> leader)
    {
        leader.Comp.LeaderActionEnts.RemoveAll(action => TerminatingOrDeleted(action));

        foreach (var actionId in leader.Comp.LeaderActions)
            EnsureAction(leader, actionId);

        // One reckoning per cult, so killing the leader does not hand it back.
        if (_cultRule.TryGetActiveRule(out var rule) && rule.FinalReckoningUsed)
            return;

        EnsureAction(leader, leader.Comp.FinalReckoningAction);
    }

    private void EnsureAction(Entity<BloodCultLeaderComponent> leader, EntProtoId actionId)
    {
        foreach (var held in leader.Comp.LeaderActionEnts)
        {
            if (MetaData(held).EntityPrototype?.ID == actionId.Id)
                return;
        }

        GrantAction(leader, actionId);
    }

    private void OnLeaderShutdown(Entity<BloodCultLeaderComponent> leader, ref ComponentShutdown args)
    {
        foreach (var action in leader.Comp.LeaderActionEnts)
        {
            if (TryComp<ActionComponent>(action, out var actionComp))
                _actions.RemoveAction(leader.Owner, (action, actionComp));
        }

        leader.Comp.LeaderActionEnts.Clear();
        leader.Comp.PulseTarget = null;

        // Whoever is no longer the leader is no longer calling anyone in.
        if (leader.Comp.ReckoningDoAfter is { } doAfter)
            _doAfter.Cancel(doAfter);

        leader.Comp.ReckoningDoAfter = null;
        leader.Comp.ReckoningAction = null;
    }

    private void GrantAction(Entity<BloodCultLeaderComponent> leader, EntProtoId actionId)
    {
        var action = _actions.AddAction(leader, actionId);
        if (action.HasValue)
            leader.Comp.LeaderActionEnts.Add(action.Value);
    }

    #endregion

    #region Final Reckoning

    /// <summary>
    ///     The click only starts the call. Handled stays false on purpose so the charge is not spent
    ///     until the chant actually finishes; the guard below is what stops it being spammed.
    /// </summary>
    private void OnFinalReckoning(Entity<BloodCultLeaderComponent> leader, ref BloodCultFinalReckoningEvent args)
    {
        if (args.Handled)
            return;

        // Standing where the rending rune can be drawn, this would just be a button that stacks the
        // whole cult on the summoning site. Refuse without spending the charge.
        if (_cultRule.CanDrawRendingRune(leader))
        {
            _popup.PopupEntity(Loc.GetString("cult-final-reckoning-too-close"), leader, leader,
                PopupType.MediumCaution);
            return;
        }

        if (leader.Comp.ReckoningDoAfter is not null)
            return;

        var doAfter = new DoAfterArgs(
            EntityManager,
            leader.Owner,
            args.DoAfterDuration,
            new BloodCultFinalReckoningDoAfterEvent(),
            leader.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BlockDuplicate = true,
            CancelDuplicate = false
        };

        if (!_doAfter.TryStartDoAfter(doAfter, out var doAfterId))
            return;

        leader.Comp.ReckoningDoAfter = doAfterId;
        leader.Comp.ReckoningAction = args.Action.Owner;

        // Final Reckoning deliberately does not complete its action until the ten-second call ends,
        // so SpeakOnAction cannot announce its opening invocation for us.
        _chat.TrySendInGameICMessage(
            leader,
            Loc.GetString("cult-spell-chant-final-reckoning"),
            InGameICChatType.Speak,
            false,
            checkRadioPrefix: false);

        _popup.PopupEntity(Loc.GetString("cult-final-reckoning-begin"), leader, leader, PopupType.Medium);
    }

    private void OnFinalReckoningDoAfter(
        Entity<BloodCultLeaderComponent> leader,
        ref BloodCultFinalReckoningDoAfterEvent args
    )
    {
        leader.Comp.ReckoningDoAfter = null;
        var spentAction = leader.Comp.ReckoningAction;
        leader.Comp.ReckoningAction = null;

        if (args.Cancelled || args.Handled)
        {
            _popup.PopupEntity(Loc.GetString("cult-final-reckoning-interrupted"), leader, leader,
                PopupType.MediumCaution);

            return;
        }

        var destination = Transform(leader.Owner).Coordinates;
        var pulled = 0;

        foreach (var summoned in GetSummonables(leader.Owner))
        {
            Teleport(summoned, destination);
            pulled++;
        }

        if (_cultRule.TryGetActiveRule(out var rule))
            rule.FinalReckoningUsed = true;

        _cultRule.NotifyCultists(Loc.GetString("cult-final-reckoning", ("amount", pulled)));

        if (spentAction is { } action)
        {
            leader.Comp.LeaderActionEnts.Remove(action);
            if (TryComp<ActionComponent>(action, out var spentComp))
                _actions.RemoveAction(leader.Owner, (action, spentComp));
        }

        args.Handled = true;
    }

    /// <summary>
    ///     Every living cultist and construct except the leader themselves.
    /// </summary>
    private List<EntityUid> GetSummonables(EntityUid leader)
    {
        var found = new List<EntityUid>();

        var cultists = EntityQueryEnumerator<BloodCultistComponent>();
        while (cultists.MoveNext(out var uid, out _))
        {
            if (uid != leader && !_mobState.IsDead(uid))
                found.Add(uid);
        }

        var constructs = EntityQueryEnumerator<ConstructComponent>();
        while (constructs.MoveNext(out var uid, out _))
        {
            if (!_mobState.IsDead(uid))
                found.Add(uid);
        }

        return found;
    }

    private void Teleport(EntityUid target, EntityCoordinates destination)
    {
        // Someone stuffed in a locker or a body bag comes out of it first, otherwise they would be
        // teleported container and all, or not at all.
        _container.TryRemoveFromContainer(target, true);

        Spawn(TeleportOutEffect, Transform(target).Coordinates);
        _transform.SetCoordinates(target, destination);
        Spawn(TeleportInEffect, destination);
    }

    #endregion

    #region Mark Target

    private void OnMarkTarget(Entity<BloodCultLeaderComponent> leader, ref BloodCultMarkTargetEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;

        // Dumont
        if (_divineIntervention.TouchSpellDenied(target))
            return;

        if (HasComp<BloodCultistComponent>(target) || HasComp<ConstructComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("cult-mark-own"), leader, leader, PopupType.MediumCaution);
            return;
        }

        var mark = EnsureComp<BloodCultMarkComponent>(target);
        mark.EndTime = _timing.CurTime + args.Duration;
        Dirty(target, mark);

        var location = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(target));
        _cultRule.NotifyCultists(Loc.GetString("cult-mark-set",
            ("name", Name(target)),
            ("location", location)));

        args.Handled = true;
    }

    #endregion

    #region Eldritch Pulse

    private void OnEldritchPulse(Entity<BloodCultLeaderComponent> leader, ref BloodCultEldritchPulseEvent args)
    {
        if (args.Handled)
            return;

        if (!_transform.InRange(Transform(leader.Owner).Coordinates, args.Target, args.Range))
        {
            _popup.PopupEntity(Loc.GetString("cult-pulse-too-far"), leader, leader, PopupType.MediumCaution);
            return;
        }

        // Second click: let go of whatever we are holding, at the spot just clicked.
        if (leader.Comp.PulseTarget is { } held && !TerminatingOrDeleted(held))
        {
            leader.Comp.PulseTarget = null;
            Teleport(held, args.Target);
            args.Handled = true;
            return;
        }

        // First click: take hold. Deliberately leaves Handled false so the cooldown only starts once
        // the pulse has actually thrown something.
        if (FindPulseTarget(args.Target, args.Whitelist) is not { } grabbed)
        {
            _popup.PopupEntity(Loc.GetString("cult-pulse-nothing"), leader, leader, PopupType.Medium);
            return;
        }

        leader.Comp.PulseTarget = grabbed;
        _popup.PopupEntity(Loc.GetString("cult-pulse-held", ("name", Name(grabbed))), leader, leader);
    }

    private EntityUid? FindPulseTarget(EntityCoordinates coords, EntityWhitelist? whitelist)
    {
        return _lookup.GetEntitiesInRange(coords, PulsePickRange)
            .Where(candidate => _whitelist.IsWhitelistPass(whitelist, candidate))
            .Select(candidate => (EntityUid?) candidate)
            .FirstOrDefault();
    }

    #endregion
}
