// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.AlertLevel;
using Content.Server.Chat.Systems;
using Content.Server.Station.Components;
using Content.Server.WhiteDream.BloodCult.Rift;
using Content.Shared.Chat;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Station.Components;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.WhiteDream.BloodCult.Gamerule;

public sealed partial class BloodCultRuleSystem
{
    private static readonly SoundSpecifier VeilTornSound =
        new SoundPathSpecifier("/Audio/WhiteDream/BloodCult/dimensional_rend.ogg");

    private const string VeilAlertLevel = "deltacult"; // Dumont

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AlertLevelSystem _alertLevel = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private BloodCultRiftSetupSystem _riftSetup = default!;

    /// <summary>
    ///     Grabs the first running cult rule, if any.
    /// </summary>
    public bool TryGetActiveRule(out BloodCultRuleComponent rule)
    {
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var comp, out _))
        {
            rule = comp;
            return true;
        }

        rule = default!;
        return false;
    }

    public bool IsVeilWeakened()
    {
        return TryGetActiveRule(out var rule) && rule.VeilWeakened;
    }

    /// <summary>
    ///     How many cultists have to chant together to tear the veil. Scales with the crew.
    /// </summary>
    public int GetMinimumCultistsForVeilRitual(BloodCultRuleComponent rule)
    {
        var aliveHumans = _mind.GetAliveHumans().Count;
        rule.MinimumCultistsForVeilRitual = Math.Max(rule.VeilRitualMinCultists,
            (int) Math.Ceiling(aliveHumans * rule.VeilRitualCultistRatio));

        return rule.MinimumCultistsForVeilRitual;
    }

    /// <summary>
    ///     The chant succeeded. Weaken the veil and start the countdown to the rift.
    /// </summary>
    public void CompleteVeilRitual(BloodCultRuleComponent rule)
    {
        if (rule.VeilWeakened)
            return;

        rule.VeilWeakened = true;
        rule.RiftSpawnTime = _timing.CurTime + rule.RiftSpawnDelay;

        _chat.DispatchGlobalAnnouncement(Loc.GetString("cult-veil-torn"),
            Loc.GetString("blood-cult-title"),
            true,
            VeilTornSound,
            Color.DarkRed);

        ForceAlertLevel(VeilAlertLevel);

        var stations = EntityQueryEnumerator<StationDataComponent, AlertLevelComponent>();
        while (stations.MoveNext(out var station, out var stationData, out _))
            StainTheSky((station, stationData));
    }

    /// <summary>
    ///     Sets the given alert level on every station.
    /// </summary>
    public void ForceAlertLevel(string level) // Dumont
    {
        var stations = EntityQueryEnumerator<StationDataComponent, AlertLevelComponent>();
        while (stations.MoveNext(out var station, out _, out var alert))
            _alertLevel.SetLevel(station, level, true, true, true, true, component: alert); // Dumont
    }

    /// <summary>
    ///     Makes a cultist shout something. Used for the ritual chants.
    /// </summary>
    public void Speak(EntityUid cultist, string message)
    {
        _chat.TrySendInGameICMessage(cultist, message, InGameICChatType.Speak, ChatTransmitRange.Normal);
    }

    /// <summary>
    ///     Every live cultist standing within <paramref name="range"/> of the given runes.
    /// </summary>
    public List<EntityUid> GetCultistsOnRunes(IEnumerable<EntityUid> runes, float range)
    {
        var found = new HashSet<EntityUid>();

        foreach (var rune in runes)
        {
            if (TerminatingOrDeleted(rune))
                continue;

            var coords = _transform.GetMapCoordinates(rune);
            foreach (var entity in _lookup.GetEntitiesInRange(coords, range))
            {
                if (!HasComp<BloodCultistComponent>(entity) || !HasComp<MobStateComponent>(entity))
                    continue;

                if (_mobState.IsDead(entity) || _mobState.IsCritical(entity))
                    continue;

                if (!_mind.TryGetMind(entity, out _, out _))
                    continue;

                found.Add(entity);
            }
        }

        return found.ToList();
    }

    /// <summary>
    ///     Popup + cult chat line for every cultist alive.
    /// </summary>
    public void NotifyCultists(string message)
    {
        var query = EntityQueryEnumerator<BloodCultistComponent>();
        while (query.MoveNext(out var uid, out _))
            _commune.AnnounceToCultist(uid, message, 12, VeilColor);
    }

    protected override void ActiveTick(
        EntityUid uid,
        BloodCultRuleComponent component,
        GameRuleComponent gameRule,
        float frameTime
    )
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        TickProgression(component);

        if (component.RiftSpawnTime is not { } spawnTime || _timing.CurTime < spawnTime)
            return;

        component.RiftSpawnTime = null;

        if (_riftSetup.TrySetupRitualSite(component) is not { } rift)
        {
            // Nowhere on the station would take it. Try again shortly rather than softlocking the round.
            component.RiftSpawnTime = _timing.CurTime + TimeSpan.FromSeconds(30);
            return;
        }

        component.Rift = rift;
        NotifyCultists(Loc.GetString("cult-rift-spawned", ("location", component.RiftLocation ?? "?")));
    }
}
