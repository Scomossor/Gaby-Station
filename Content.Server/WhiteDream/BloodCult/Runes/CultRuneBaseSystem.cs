// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Goobstation.Common.Religion;
using Content.Goobstation.Shared.Bible;
using Content.Server.Chat.Systems;
using Content.Server.Chemistry.Components;
using Content.Server.DoAfter;
using Content.Server.Fluids.Components;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.WhiteDream.BloodCult.Empower;
using Content.Server.WhiteDream.BloodCult.Gamerule;
using Content.Shared.Bible.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Fluids.Components;
using Content.Shared.Ghost;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Content.Shared.UserInterface;
using Content.Shared.WhiteDream.BloodCult;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Content.Shared.WhiteDream.BloodCult.Constructs;
using Content.Shared.WhiteDream.BloodCult.Runes;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Server.WhiteDream.BloodCult.Runes;

public sealed partial class CultRuneBaseSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _protoManager = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private BloodCultRuleSystem _cultRule = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private DoAfterSystem _doAfter = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        // Drawing rune
        SubscribeLocalEvent<RuneDrawerComponent, BoundUIOpenedEvent>(OnRuneDrawerOpened);
        SubscribeLocalEvent<RuneDrawerComponent, RuneDrawerSelectedMessage>(OnRuneSelected);
        SubscribeLocalEvent<BloodCultistComponent, DrawRuneDoAfter>(OnDrawRune);

        // Erasing rune
        SubscribeLocalEvent<CultRuneBaseComponent, InteractUsingEvent>(EraseOnInteractUsing);
        SubscribeLocalEvent<CultRuneBaseComponent, RuneEraseDoAfterEvent>(OnRuneErase);
        SubscribeLocalEvent<CultRuneBaseComponent, StartCollideEvent>(EraseOnCollding);

        // Rune invoking
        SubscribeLocalEvent<CultRuneBaseComponent, ActivateInWorldEvent>(OnRuneActivate);

        SubscribeLocalEvent<CultRuneBaseComponent, ExamineAttemptEvent>(OnRuneExaminaAttempt);
    }

    #region EventHandlers

    private void OnRuneDrawerOpened(Entity<RuneDrawerComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!Equals(args.UiKey, RuneDrawerBuiKey.Key))
            return;

        var availableRunes = new List<ProtoId<RuneSelectorPrototype>>();
        var runeSelectorArray = _protoManager.EnumeratePrototypes<RuneSelectorPrototype>().OrderBy(r => r.ID).ToArray();
        foreach (var runeSelector in runeSelectorArray)
        {
            // Dumont
            if (runeSelector.RequireTargetDead && !_cultRule.IsObjectiveFinished() ||
                _cultRule.GetRequiredCultists(runeSelector) > _cultRule.GetTotalCultists())
                continue;

            // WhiteDream - leader-only runes stay hidden from everyone else.
            if (runeSelector.RequireLeader && !HasComp<BloodCultLeaderComponent>(args.Actor))
                continue;

            if (runeSelector.RequireVeilWeakened && !_cultRule.IsVeilWeakened())
                continue;

            availableRunes.Add(runeSelector.ID);
        }

        _ui.SetUiState(ent.Owner, RuneDrawerBuiKey.Key, new RuneDrawerMenuState(availableRunes));
    }

    private void OnRuneSelected(Entity<RuneDrawerComponent> ent, ref RuneDrawerSelectedMessage args)
    {
        if (!_protoManager.TryIndex(args.SelectedRune, out var runeSelector) || !CanDrawRune(args.Actor))
            return;

        if (runeSelector.RequireTargetDead && !_cultRule.CanDrawRendingRune(args.Actor))
        {
            _popup.PopupEntity(Loc.GetString("cult-rune-cant-draw-rending"), args.Actor, args.Actor);
            return;
        }

        // WhiteDream - leader-only runes.
        if (runeSelector.RequireLeader && !HasComp<BloodCultLeaderComponent>(args.Actor))
        {
            _popup.PopupEntity(Loc.GetString("cult-rune-cant-draw-leader"), args.Actor, args.Actor);
            return;
        }

        if (runeSelector.RequireVeilWeakened && !_cultRule.IsVeilWeakened())
        {
            _popup.PopupEntity(Loc.GetString("cult-rune-cant-draw-veil"), args.Actor, args.Actor);
            return;
        }

        var timeToDraw = runeSelector.DrawTime;
        if (TryComp(args.Actor, out BloodCultEmpoweredComponent? empowered))
            timeToDraw *= empowered.RuneTimeMultiplier;

        var ev = new DrawRuneDoAfter
        {
            Rune = args.SelectedRune,
            EndDrawingSound = ent.Comp.EndDrawingSound
        };

        var argsDoAfterEvent = new DoAfterArgs(EntityManager, args.Actor, timeToDraw, ev, args.Actor)
        {
            BreakOnMove = true,
            NeedHand = true
        };

        if (_doAfter.TryStartDoAfter(argsDoAfterEvent))
            _audio.PlayPvs(ent.Comp.StartDrawingSound, args.Actor, AudioParams.Default.WithMaxDistance(2f));
    }

    private void OnDrawRune(Entity<BloodCultistComponent> ent, ref DrawRuneDoAfter args)
    {
        if (args.Cancelled || !_protoManager.TryIndex(args.Rune, out var runeSelector))
            return;

        DealDamage(args.User, runeSelector.DrawDamage);

        _audio.PlayPvs(args.EndDrawingSound, args.User, AudioParams.Default.WithMaxDistance(2f));
        var runeEnt = SpawnRune(args.User, runeSelector.Prototype);
        // Dumont
        if (TryComp(runeEnt, out CultRuneBaseComponent? rune)
            && rune.TriggerRendingMarkers
            && !_cultRule.TryConsumeNearestMarker(args.User))
        {
            // Another drawing may have finished on this site during our do-after. Never leave the
            // newly spawned rending rune behind unless this attempt actually spent a site.
            QueueDel(runeEnt);
            return;
        }

        var ev = new AfterRunePlaced(args.User);
        RaiseLocalEvent(runeEnt, ev);
    }

    private void EraseOnInteractUsing(Entity<CultRuneBaseComponent> rune, ref InteractUsingEvent args)
    {
        if (!rune.Comp.CanBeErased)
            return;

        // Logic for bible erasing
        if (TryComp<BibleComponent>(args.Used, out var bible) && HasComp<BibleUserComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString("cult-rune-erased"), rune, args.User);
            _audio.PlayPvs(bible.HealSoundPath, args.User);
            Del(args.Target);
            return;
        }

        if (!TryComp(args.Used, out RuneDrawerComponent? runeDrawer))
            return;

        var argsDoAfterEvent =
            new DoAfterArgs(EntityManager, args.User, runeDrawer.EraseTime, new RuneEraseDoAfterEvent(), rune)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true
            };

        if (_doAfter.TryStartDoAfter(argsDoAfterEvent))
            _popup.PopupEntity(Loc.GetString("cult-rune-started-erasing"), rune, args.User);
    }

    private void OnRuneErase(Entity<CultRuneBaseComponent> ent, ref RuneEraseDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        _popup.PopupEntity(Loc.GetString("cult-rune-erased"), ent, args.User);
        Del(ent.Owner);
    }

    private void EraseOnCollding(Entity<CultRuneBaseComponent> rune, ref StartCollideEvent args)
    {
        if (!rune.Comp.CanBeErased ||
            !TryComp<SolutionContainerManagerComponent>(args.OtherEntity, out var solutionContainer) || // Trauma - renamed
            !HasComp<VaporComponent>(args.OtherEntity) && !HasComp<SprayComponent>(args.OtherEntity))
            return;

        if (_solutionContainer.EnumerateSolutions((args.OtherEntity, solutionContainer))
            .Any(solution => solution.Solution.Comp.Solution.ContainsPrototype(rune.Comp.HolyWaterPrototype)))
            Del(rune.Owner);
    }

    private void OnRuneActivate(Entity<CultRuneBaseComponent> rune, ref ActivateInWorldEvent args)
    {
        var runeCoordinates = Transform(rune).Coordinates;
        var userCoordinates = Transform(args.User).Coordinates;
        if (args.Handled || !HasComp<BloodCultistComponent>(args.User) ||
            !userCoordinates.TryDistance(EntityManager, runeCoordinates, out var distance) ||
            distance > rune.Comp.RuneActivationRange)
            return;

        args.Handled = true;

        var cultists = GatherCultists(rune, rune.Comp.RuneActivationRange);
        if (cultists.Count < rune.Comp.RequiredInvokers)
        {
            _popup.PopupEntity(Loc.GetString("cult-rune-not-enough-cultists"), rune, args.User);
            return;
        }

        var tryInvokeEv = new TryInvokeCultRuneEvent(args.User, cultists);
        RaiseLocalEvent(rune, tryInvokeEv);
        if (tryInvokeEv.Cancelled)
            return;

        foreach (var cultist in cultists)
        {
            DealDamage(cultist, rune.Comp.ActivationDamage);
            _chat.TrySendInGameICMessage(
                cultist,
                rune.Comp.InvokePhrase,
                rune.Comp.InvokeChatType,
                false,
                checkRadioPrefix: false);
        }
    }

    private void OnRuneExaminaAttempt(Entity<CultRuneBaseComponent> rune, ref ExamineAttemptEvent args)
    {
        if (!HasComp<BloodCultistComponent>(args.Examiner) && !HasComp<ConstructComponent>(args.Examiner) &&
            !HasComp<GhostComponent>(args.Examiner))
            args.Cancel();
    }

    #endregion

    private EntityUid SpawnRune(EntityUid user, EntProtoId rune)
    {
        var transform = Transform(user);
        var snappedLocalPosition = new Vector2(
            MathF.Floor(transform.LocalPosition.X) + 0.5f,
            MathF.Floor(transform.LocalPosition.Y) + 0.5f);
        var spawnPosition = _transform.GetMapCoordinates(user);
        var runeEntity = Spawn(rune, spawnPosition);
        _transform.SetLocalPosition(runeEntity, snappedLocalPosition);

        return runeEntity;
    }

    private bool CanDrawRune(EntityUid uid)
    {
        var transform = Transform(uid);
        var gridUid = transform.GridUid;
        if (!gridUid.HasValue)
        {
            _popup.PopupEntity(Loc.GetString("cult-rune-cant-draw"), uid, uid);
            return false;
        }

        if (!TryComp<MapGridComponent>(gridUid.Value, out var grid))
        {
            _popup.PopupEntity(Loc.GetString("cult-cant-draw-rune"), uid, uid);
            return false;
        }

        if (_map.TryGetTileRef(gridUid.Value, grid, transform.Coordinates, out _))
            return true;

        _popup.PopupEntity(Loc.GetString("cult-cant-draw-rune"), uid, uid);
        return false;
    }

    private void DealDamage(EntityUid user, DamageSpecifier? damage = null)
    {
        if (damage is null)
            return;

        var newDamage = BloodCultDamage.WithoutWounds(damage);
        if (TryComp(user, out BloodCultEmpoweredComponent? empowered))
        {
            foreach (var (key, value) in newDamage.DamageDict)
                newDamage.DamageDict[key] = value * empowered.RuneDamageMultiplier;
        }

        _damageable.TryChangeDamage(user, newDamage, true);
    }
}
