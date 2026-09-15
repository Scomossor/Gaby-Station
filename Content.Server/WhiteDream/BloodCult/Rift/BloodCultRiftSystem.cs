// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Maths.FixedPoint;
using Content.Server.Audio;
using Content.Server.Body.Systems;
using Content.Server.Lightning; // Dumont
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.WhiteDream.BloodCult.Commune;
using Content.Server.WhiteDream.BloodCult.Gamerule;
using Content.Server.WhiteDream.BloodCult.Runes;
using Content.Server._EinsteinEngines.Language;
using Content.Shared.Anomaly;
using Content.Shared.Audio;
using Content.Shared.Camera; // Dumont
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Gibbing;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems; // Dumont
using Robust.Shared.Random;

namespace Content.Server.WhiteDream.BloodCult.Rift;

/// <summary>
///     Makes the rift bleed, and runs the final summoning chant on the runes around it.
/// </summary>
public sealed partial class BloodCultRiftSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;

    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private BloodCultRuleSystem _cultRule = default!;
    [Dependency] private BloodCultCommuneSystem _commune = default!;
    [Dependency] private LanguageSystem _language = default!;
    [Dependency] private BodySystem _body = default!; // Dumont
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private ServerGlobalSoundSystem _sound = default!;
    // Dumont changes start
    [Dependency] private LightningSystem _lightning = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedCameraRecoilSystem _recoil = default!;
    // Dumont end
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FinalSummoningRuneComponent, TryInvokeCultRuneEvent>(OnFinalRuneInvoked);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BloodCultRiftComponent>();
        while (query.MoveNext(out var uid, out var rift))
        {
            rift.TimeUntilNextPulse -= frameTime;
            if (rift.TimeUntilNextPulse <= 0f)
            {
                Pulse(uid, rift);
                rift.TimeUntilNextPulse = rift.PulseInterval;
            }

            rift.TimeUntilNextGuardian -= frameTime;
            if (rift.TimeUntilNextGuardian <= 0f)
                TrySpawnGuardian(uid, rift);

            if (!rift.RitualInProgress)
                continue;

            rift.TimeUntilNextChant -= frameTime;
            if (rift.TimeUntilNextChant <= 0f)
                ProcessChantStep(uid, rift);
        }
    }

    /// <summary>
    ///     Tops the pool back up so the rift keeps spilling a slowly growing ocean of blood.
    /// </summary>
    private void Pulse(EntityUid uid, BloodCultRiftComponent rift)
    {
        if (_solution.TryGetSolution(uid, BloodCultRiftComponent.SolutionName, out var solutionEnt, out _))
            _solution.TryAddReagent(solutionEnt.Value,
                BloodCultRiftComponent.Reagent,
                FixedPoint2.New(rift.BloodPerPulse),
                out _);

        if (HasComp<AppearanceComponent>(uid))
            _appearance.SetData(uid, AnomalyVisualLayers.Animated, true);
    }

    #region Guardians

    /// <summary>
    ///     Something crawls out of the wound, up to <see cref="BloodCultRiftComponent.TotalGuardians"/> times.
    /// </summary>
    private void TrySpawnGuardian(EntityUid riftUid, BloodCultRiftComponent rift)
    {
        rift.TimeUntilNextGuardian = rift.RitualInProgress
            ? rift.RitualGuardianInterval
            : rift.GuardianInterval;

        if (rift.GuardiansSpawned >= rift.TotalGuardians)
            return;

        rift.Guardians.RemoveAll(guardian => TerminatingOrDeleted(guardian) || _mobState.IsDead(guardian));

        var cap = rift.RitualInProgress ? rift.RitualMaxGuardians : rift.MaxGuardians;
        if (rift.Guardians.Count >= cap)
            return;

        var coordinates = Transform(riftUid).Coordinates
            .Offset(_random.NextVector2(rift.GuardianSpawnRange));

        rift.Guardians.Add(Spawn(rift.GuardianProto, coordinates));
        rift.GuardiansSpawned++;
    }

    #endregion

    #region Ritual

    private void OnFinalRuneInvoked(Entity<FinalSummoningRuneComponent> rune, ref TryInvokeCultRuneEvent args)
    {
        if (rune.Comp.Rift is not { } riftUid || !TryComp<BloodCultRiftComponent>(riftUid, out var rift))
        {
            args.Cancel();
            return;
        }

        if (rift.RitualInProgress)
        {
            _popup.PopupEntity(Loc.GetString("cult-final-ritual-already"), rune, args.User, PopupType.MediumCaution);
            args.Cancel();
            return;
        }

        if (!_cultRule.IsObjectiveFinished())
        {
            _popup.PopupEntity(Loc.GetString("cult-rending-target-alive"), rune, args.User, PopupType.LargeCaution);
            args.Cancel();
            return;
        }

        var participants = GetParticipants(rift);
        if (participants.Count < rift.RequiredCultists)
        {
            _popup.PopupEntity(
                Loc.GetString("cult-final-ritual-not-enough",
                    ("current", participants.Count),
                    ("required", rift.RequiredCultists)),
                rune,
                args.User,
                PopupType.LargeCaution);
            args.Cancel();
            return;
        }

        rift.RitualInProgress = true;
        rift.RitualStartingRequiredCultists = rift.RequiredCultists;
        rift.ChantsInCycle = 0;
        rift.SacrificesDone = 0;
        rift.PendingSacrifice = null;
        rift.TimeUntilNextChant = 0f;

        StartMusic(riftUid, rift);
        _cultRule.NotifyCultists(Loc.GetString("cult-final-ritual-started"));
        ProcessChantStep(riftUid, rift);
    }

    private void ProcessChantStep(EntityUid riftUid, BloodCultRiftComponent rift)
    {
        var participants = GetParticipants(rift);
        if (participants.Count < rift.RequiredCultists)
        {
            AbortRitual(riftUid, rift, participants.Count);
            return;
        }

        // Whoever draws the long chant is the next to be taken. Foreshadowing is half the fun.
        if (rift.PendingSacrifice is not { } pending || !participants.Contains(pending))
            rift.PendingSacrifice = _random.Pick(participants);

        // Everyone chants. The marked one always says more, and the whole cult says more as the
        // ritual tightens, so it builds from a murmur into a frenzy instead of cutting abruptly.
        var leaderChant = _commune.GenerateChant(rift.LeaderChantWords);
        foreach (var cultist in participants)
        {
            var line = cultist == rift.PendingSacrifice
                ? leaderChant
                : _commune.GenerateChant(rift.FollowerChantWords);

            _cultRule.Speak(cultist, line);
        }

        rift.ChantsInCycle++;

        var cycle = rift.CurrentCycle;
        if (rift.ChantsInCycle <= cycle.Count)
        {
            rift.TimeUntilNextChant = cycle[rift.ChantsInCycle - 1];
            return;
        }

        // The cycle finished, so the veil takes the one who was chanting.
        if (!TakeSacrifice(riftUid, rift))
        {
            // Couldn't take them, start the cycle over rather than stalling.
            rift.PendingSacrifice = null;
            rift.ChantsInCycle = 0;
            rift.TimeUntilNextChant = 1f;
            return;
        }

        rift.SacrificesDone++;
        rift.RequiredCultists = Math.Max(1, rift.RequiredCultists - 1);
        rift.PendingSacrifice = null;
        rift.ChantsInCycle = 0;
        rift.TimeUntilNextChant = 1f;

        // Dumont
        if (rift.SacrificesDone == 1)
            _cultRule.ForceAlertLevel(rift.FirstSacrificeAlertLevel);

        _cultRule.NotifyCultists(Loc.GetString("cult-final-ritual-sacrifice",
            ("done", rift.SacrificesDone),
            ("required", rift.RequiredSacrifices)));

        if (rift.SacrificesDone >= rift.RequiredSacrifices)
            SummonNarsie(riftUid, rift);
    }

    private void AbortRitual(EntityUid riftUid, BloodCultRiftComponent rift, int present)
    {
        rift.RitualInProgress = false;
        rift.ChantsInCycle = 0;
        // Each completed sacrifice lowers the live requirement. Restore the exact value from the
        // beginning of the attempt: adding SacrificesDone is wrong once the requirement hits its
        // lower bound of one.
        if (rift.RitualStartingRequiredCultists is { } startingRequirement)
            rift.RequiredCultists = startingRequirement;

        rift.RitualStartingRequiredCultists = null;
        rift.SacrificesDone = 0;
        rift.PendingSacrifice = null;
        rift.TimeUntilNextChant = 0f;
        StopMusic(riftUid, rift);

        _popup.PopupEntity(Loc.GetString("cult-final-ritual-broken"), riftUid, PopupType.LargeCaution);
        _cultRule.NotifyCultists(Loc.GetString("cult-final-ritual-failed",
            ("current", present),
            ("required", rift.RequiredCultists)));
    }

    /// <summary>
    ///     Nar'Sie takes the one who was chanting and gives them back as a herald. Being offered on
    ///     the rift is a promotion, not a soulstone - only the mindless end up as shards.
    /// </summary>
    private bool TakeSacrifice(EntityUid riftUid, BloodCultRiftComponent rift)
    {
        if (rift.PendingSacrifice is not { } victim || TerminatingOrDeleted(victim))
            return false;

        var coordinates = Transform(victim).Coordinates;

        // Dumont
        _lightning.ShootLightning(riftUid, victim, rift.SacrificeLightningProto, false);

        if (!_mind.TryGetMind(victim, out var mindId, out _))
        {
            Spawn(rift.SoulShardGhostProto, coordinates);
        }
        else
        {
            var harvester = Spawn(rift.HarvesterProto, coordinates);
            _mind.TransferTo(mindId, harvester);
            _mind.UnVisit(mindId);
            _language.UpdateEntityLanguages(harvester);
        }

        _body.GibBody(victim); // Dumont

        // Dumont
        _cultRule.FlickerStationLights(rift.SacrificeFlickerTime);
        ShakeScreens(riftUid, rift);

        return true;
    }

    /// <summary>
    ///     Shakes the screen of every chanting cultist and of anyone near the rift.
    /// </summary>
    private void ShakeScreens(EntityUid riftUid, BloodCultRiftComponent rift)
    {
        foreach (var cultist in GetParticipants(rift))
        {
            if (!TerminatingOrDeleted(cultist))
                _recoil.KickCamera(cultist, _random.NextVector2(0.5f, 1.5f));
        }

        if (!TerminatingOrDeleted(riftUid))
            _recoil.KickCamera(riftUid, _random.NextVector2(0.5f, 1.5f));
    }

    private void SummonNarsie(EntityUid riftUid, BloodCultRiftComponent rift)
    {
        rift.RitualInProgress = false;
        rift.RitualStartingRequiredCultists = null;
        rift.TimeUntilNextChant = 0f;
        StopMusic(riftUid, rift);

        // Dumont
        _sound.PlayGlobalOnStation(riftUid, _audio.ResolveSound(rift.SummonSound));

        RaiseLocalEvent(new BloodCultNarsieSummoned());
        Spawn(rift.NarsiePrototype, _transform.GetMapCoordinates(riftUid));
    }

    private void StartMusic(EntityUid riftUid, BloodCultRiftComponent rift)
    {
        if (rift.MusicPlaying)
            return;

        _sound.DispatchStationEventMusic(riftUid, rift.RitualMusic, StationEventMusicType.BloodCult);
        rift.MusicPlaying = true;
    }

    private void StopMusic(EntityUid riftUid, BloodCultRiftComponent rift)
    {
        if (!rift.MusicPlaying)
            return;

        _sound.StopStationEventMusic(riftUid, StationEventMusicType.BloodCult);
        rift.MusicPlaying = false;
    }

    private List<EntityUid> GetParticipants(BloodCultRiftComponent rift)
    {
        return _cultRule.GetCultistsOnRunes(rift.SummoningRunes, rift.RuneRange);
    }

    #endregion
}
