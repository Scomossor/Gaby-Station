// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Pinpointer;
using Content.Server.Popups;
using Content.Server.WhiteDream.BloodCult.Commune;
using Content.Server.WhiteDream.BloodCult.Gamerule;
using Content.Shared.Popups;
using Content.Shared.WhiteDream.BloodCult.Runes;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.WhiteDream.BloodCult.Runes.Rending;

public sealed partial class CultRuneRendingSystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private BloodCultCommuneSystem _commune = default!;
    [Dependency] private BloodCultRuleSystem _cultRule = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private TransformSystem _transform = default!;

    /// <summary>
    ///     The looping ritual track, shared by every rune taking part.
    /// </summary>
    private Entity<AudioComponent>? _ritualAudio;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultRuneRendingComponent, AfterRunePlaced>(OnRendingRunePlaced);
        SubscribeLocalEvent<CultRuneRendingComponent, TryInvokeCultRuneEvent>(OnRendingRuneInvoked);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CultRuneRendingComponent>();
        while (query.MoveNext(out var uid, out var rune))
        {
            if (!rune.RitualInProgress)
                continue;

            rune.TimeUntilNextChant -= frameTime;
            if (rune.TimeUntilNextChant <= 0f)
                ProcessChantStep(uid, rune);
        }
    }

    private void OnRendingRunePlaced(Entity<CultRuneRendingComponent> rune, ref AfterRunePlaced args)
    {
        var position = _transform.GetMapCoordinates(rune);
        var message = Loc.GetString(
            "cult-rending-drawing-finished",
            ("location", FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(position))));

        _chat.DispatchGlobalAnnouncement(
            message,
            Loc.GetString("blood-cult-title"),
            true,
            rune.Comp.FinishedDrawingAudio,
            Color.DarkRed);

        // The station flinches when the veil is scratched.
        _cultRule.FlickerStationLights(TimeSpan.FromSeconds(8));
    }

    private void OnRendingRuneInvoked(Entity<CultRuneRendingComponent> rune, ref TryInvokeCultRuneEvent args)
    {
        if (!_cultRule.TryGetActiveRule(out var cultRule))
        {
            args.Cancel();
            return;
        }

        if (!_cultRule.IsObjectiveFinished())
        {
            _popup.PopupEntity(Loc.GetString("cult-rending-target-alive"), rune, args.User, PopupType.LargeCaution);
            args.Cancel();
            return;
        }

        if (cultRule.VeilWeakened)
        {
            _popup.PopupEntity(Loc.GetString("cult-veil-ritual-already-completed"),
                rune,
                args.User,
                PopupType.MediumCaution);
            args.Cancel();
            return;
        }

        // Only one ritual at a time, anywhere.
        var running = EntityQueryEnumerator<CultRuneRendingComponent>();
        while (running.MoveNext(out _, out var other))
        {
            if (!other.RitualInProgress)
                continue;

            _popup.PopupEntity(Loc.GetString("cult-veil-ritual-already-in-progress"),
                rune,
                args.User,
                PopupType.MediumCaution);
            args.Cancel();
            return;
        }

        var mapUid = Transform(rune).MapUid;
        if (mapUid is not { Valid: true })
        {
            args.Cancel();
            return;
        }

        var required = _cultRule.GetMinimumCultistsForVeilRitual(cultRule);
        var present = GetParticipants(mapUid.Value, rune.Comp.ParticipantRange).Count;

        if (present < required)
        {
            _popup.PopupEntity(
                Loc.GetString("cult-veil-ritual-not-enough-cultists",
                    ("current", present),
                    ("required", required)),
                rune,
                args.User,
                PopupType.LargeCaution);
            args.Cancel();
            return;
        }

        rune.Comp.RitualInProgress = true;
        rune.Comp.RitualMap = mapUid;
        rune.Comp.CurrentChantStep = 0;
        rune.Comp.TimeUntilNextChant = rune.Comp.ChantInterval;

        _chat.DispatchGlobalAnnouncement(
            Loc.GetString("cult-rending-started",
                ("location",
                    FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(rune.Owner)))),
            Loc.GetString("blood-cult-title"),
            false,
            colorOverride: Color.DarkRed);

        _cultRule.NotifyCultists(Loc.GetString("cult-veil-ritual-started", ("required", required)));

        // Dumont
        SetRunesActive(true);
        _ritualAudio ??= _audio.PlayGlobal(rune.Comp.SummonAudio,
            Filter.Broadcast(),
            false,
            AudioParams.Default.WithLoop(true));

        // Chant once immediately so the ritual has a visible start.
        ProcessChantStep(rune, rune.Comp);
    }

    private void ProcessChantStep(EntityUid runeUid, CultRuneRendingComponent rune)
    {
        if (!_cultRule.TryGetActiveRule(out var cultRule) || rune.RitualMap is not { Valid: true } mapUid)
        {
            EndRitual(rune);
            return;
        }

        var participants = GetParticipants(mapUid, rune.ParticipantRange);
        var required = cultRule.MinimumCultistsForVeilRitual;

        if (participants.Count < required)
        {
            _popup.PopupEntity(Loc.GetString("cult-veil-ritual-broken"), runeUid, PopupType.LargeCaution);
            _cultRule.NotifyCultists(Loc.GetString("cult-veil-ritual-failed",
                ("current", participants.Count),
                ("required", required)));

            _chat.DispatchGlobalAnnouncement(Loc.GetString("cult-rending-prevented"),
                Loc.GetString("blood-cult-title"),
                false,
                colorOverride: Color.DarkRed);

            EndRitual(rune);
            return;
        }

        foreach (var cultist in participants)
            _cultRule.Speak(cultist, _commune.GenerateChant(3));

        _cultRule.FlickerStationLights(TimeSpan.FromSeconds(rune.ChantInterval + 2f));

        rune.CurrentChantStep++;

        if (rune.CurrentChantStep < rune.TotalChantSteps)
        {
            rune.TimeUntilNextChant = rune.ChantInterval;
            return;
        }

        _cultRule.CompleteVeilRitual(cultRule);
        _cultRule.NotifyCultists(Loc.GetString("cult-veil-ritual-success"));
        MarkAllCompleted();
        EndRitual(rune);
    }

    /// <summary>
    ///     Every live cultist standing on any rending rune on this map.
    /// </summary>
    private List<EntityUid> GetParticipants(EntityUid mapUid, float range)
    {
        var runes = new List<EntityUid>();
        var query = EntityQueryEnumerator<CultRuneRendingComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapUid == mapUid)
                runes.Add(uid);
        }

        return _cultRule.GetCultistsOnRunes(runes, range);
    }

    private void EndRitual(CultRuneRendingComponent rune)
    {
        rune.RitualInProgress = false;
        rune.RitualMap = null;
        rune.CurrentChantStep = 0;
        rune.TimeUntilNextChant = 0f;

        if (EntityQuery<CultRuneRendingComponent>().Any(other => other.RitualInProgress))
            return;

        _audio.Stop(_ritualAudio);
        _ritualAudio = null;
        SetRunesActive(false);
    }

    private void MarkAllCompleted()
    {
        var query = EntityQueryEnumerator<CultRuneRendingComponent>();
        while (query.MoveNext(out _, out var rune))
        {
            rune.RitualCompleted = true;
            rune.RitualInProgress = false;
            rune.RitualMap = null;
            rune.CurrentChantStep = 0;
            rune.TimeUntilNextChant = 0f;
        }
    }

    private void SetRunesActive(bool active)
    {
        var query = EntityQueryEnumerator<CultRuneRendingComponent>();
        while (query.MoveNext(out var uid, out _))
            _appearance.SetData(uid, RendingRuneVisuals.Active, active);
    }
}
