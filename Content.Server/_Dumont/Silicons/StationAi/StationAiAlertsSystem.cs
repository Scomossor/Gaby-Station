// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Pinpointer;
using Content.Server.Station.Systems;
using Content.Shared._Dumont.Silicons.StationAi;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Doors;
using Content.Shared.Doors.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Mind.Components;
using Content.Shared.Silicons.StationAi;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Dumont.Silicons.StationAi;

/// <summary>
/// dá pra IA um monitor do que ela deveria reagir: alarme de atmos, de incêndio e
/// tripulante barrado numa porta. o monitor é fila de trabalho, não espelho.. toda linha
/// tem destino: Ir resolve e a fonte entra em cooldown de 1 minuto, Ocultar some com a
/// linha até o problema disparar de novo, e sem ação nenhuma ela expira sozinha
/// o aviso no chat conta que aconteceu com a janela fechada, o monitor é onde se resolve
/// </summary>
public sealed class StationAiAlertsSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly SharedDoorSystem _doors = default!;
    [Dependency] private readonly SharedStationAiSystem _stationAi = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly StationAiWaypointSystem _waypoints = default!;

    /// <summary>
    /// quanto tempo uma área precisa ficar quieta pra avisar a IA no chat de novo
    /// não afeta o monitor, que sempre mostra a situação atual.
    /// </summary>
    private static readonly TimeSpan ChatCooldown = TimeSpan.FromSeconds(60);

    /// <summary>
    /// esbarrar em porta repete rápido então a mesma pessoa na mesma porta só vale
    /// mencionar de vez em quando
    /// </summary>
    private static readonly TimeSpan KnockCooldown = TimeSpan.FromSeconds(20);

    /// <summary>
    /// pedido de porta vence sozinho.. ou alguém abriu ou a pessoa foi embora
    /// </summary>
    private static readonly TimeSpan KnockLifetime = TimeSpan.FromSeconds(120);

    /// <summary>
    /// linha que a IA não resolveu nem ocultou expira sozinha
    /// </summary>
    private static readonly TimeSpan AlertLifetime = TimeSpan.FromSeconds(300);

    /// <summary>
    /// Ir marca como resolvido, a fonte fica quieta esse tempo antes de poder voltar
    /// </summary>
    private static readonly TimeSpan ResolveCooldown = TimeSpan.FromSeconds(60);

    private static readonly Color DangerColor = Color.FromHex("#ff4b4b");
    private static readonly Color WarningColor = Color.FromHex("#ffa500");
    private static readonly Color DoorColor = Color.FromHex("#5ed7aa");

    private readonly Dictionary<EntityUid, ActiveAlert> _active = new();
    private readonly Dictionary<(EntityUid Door, EntityUid User), Knock> _knocks = new();
    private readonly Dictionary<(EntityUid? Station, string Area, AtmosAlarmType Type), TimeSpan> _lastChat = new();

    /// <summary>
    /// o que cada IA ocultou ou resolveu. Instance esconde tudo que começou até aquele
    /// momento, Until esconde qualquer coisa da fonte até vencer o cooldown
    /// </summary>
    private readonly Dictionary<EntityUid, Dictionary<NetEntity, Suppression>> _suppressed = new();

    private readonly record struct ActiveAlert(string Area, AiAlertKind Kind, AiAlertSeverity Severity, EntityUid? Station, TimeSpan At);

    private readonly record struct Knock(string Area, string Who, EntityUid? Station, TimeSpan FirstAt, TimeSpan At);

    private readonly record struct Suppression(TimeSpan? Instance, TimeSpan? Until);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosAlarmableComponent, AtmosAlarmEvent>(OnAtmosAlarm);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        SubscribeLocalEvent<StationAiWhitelistComponent, BeforeDoorOpenedEvent>(OnDoorAttempt);

        SubscribeLocalEvent<StationAiHeldComponent, ToggleAiAlertsScreenEvent>(OnToggleScreen);
        SubscribeLocalEvent<StationAiHeldComponent, AiAlertWarpMessage>(OnWarp);
        SubscribeLocalEvent<StationAiHeldComponent, AiAlertDismissMessage>(OnDismiss);
        SubscribeLocalEvent<StationAiHeldComponent, AiAlertsRefreshMessage>(OnRefresh);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _active.Clear();
        _knocks.Clear();
        _lastChat.Clear();
        _suppressed.Clear();
    }

    private void OnDoorAttempt(Entity<StationAiWhitelistComponent> ent, ref BeforeDoorOpenedEvent args)
    {
        if (args.User is not { } user)
            return;

        if (_doors.HasAccess(ent.Owner, user))
            return;

        if (HasComp<StationAiHeldComponent>(user) || HasComp<StationAiWhitelistComponent>(user))
            return;

        // bicho não pede porta. sem esse filtro os ratos enchem o monitor e o refresh
        // constante ainda matava o clique do Ir, a linha era reconstruída no meio do press
        if (!TryComp<MindContainerComponent>(user, out var mindContainer) || !mindContainer.HasMind)
            return;

        var now = _timing.CurTime;
        var key = (ent.Owner, user);

        // esbarrar de novo renova a linha. FirstAt identifica o pedido, At estica a validade
        var isFresh = !_knocks.TryGetValue(key, out var previous) || now > previous.At + KnockLifetime;
        var onCooldown = !isFresh && now < previous.At + KnockCooldown;

        var station = _station.GetOwningStation(ent.Owner);
        var area = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString((ent.Owner, null)));
        var who = FormattedMessage.RemoveMarkupPermissive(Name(user));

        _knocks[key] = new Knock(area, who, station, isFresh ? now : previous.FirstAt, now);

        // renovação não muda nada visível, só pedido novo redesenha o monitor
        if (isFresh)
            RefreshOpenMonitors();

        if (onCooldown)
            return;

        var recipients = GetAiFilter(station);
        if (recipients.Count > 0)
        {
            _chat.DispatchFilteredAnnouncement(
                recipients,
                Loc.GetString("station-ai-door-knock", ("who", who), ("area", area)),
                source: ent.Owner,
                sender: Loc.GetString("station-ai-alarm-sender"),
                playSound: false,
                colorOverride: DoorColor);
        }
    }

    private void OnAtmosAlarm(EntityUid uid, AtmosAlarmableComponent component, AtmosAlarmEvent args)
    {
        var isFire = HasComp<FireAlarmComponent>(uid);
        if (!isFire && !HasComp<AirAlarmComponent>(uid))
            return;

        if (args.AlarmType is AtmosAlarmType.Warning or AtmosAlarmType.Danger)
        {
            var station = _station.GetOwningStation(uid);

            var area = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString((uid, null)));

            var severity = args.AlarmType == AtmosAlarmType.Danger
                ? AiAlertSeverity.Danger
                : AiAlertSeverity.Warning;

            _active[uid] = new ActiveAlert(area, isFire ? AiAlertKind.Fire : AiAlertKind.Atmos, severity, station, _timing.CurTime);
            PingChat(uid, station, area, isFire, args.AlarmType);
        }
        else
        {
            _active.Remove(uid);
        }

        RefreshOpenMonitors();
    }

    private void PingChat(EntityUid source, EntityUid? station, string area, bool isFire, AtmosAlarmType type)
    {
        var now = _timing.CurTime;
        var key = (station, area, type);
        if (_lastChat.TryGetValue(key, out var last) && now < last + ChatCooldown)
            return;

        _lastChat[key] = now;

        var recipients = GetAiFilter(station);
        if (recipients.Count == 0)
            return;

        var message = Loc.GetString(
            type == AtmosAlarmType.Danger ? "station-ai-alarm-danger" : "station-ai-alarm-warning",
            ("kind", Loc.GetString(isFire ? "station-ai-alarm-kind-fire" : "station-ai-alarm-kind-atmos")),
            ("area", area));

        _chat.DispatchFilteredAnnouncement(
            recipients,
            message,
            source: source,
            sender: Loc.GetString("station-ai-alarm-sender"),
            playSound: false,
            colorOverride: type == AtmosAlarmType.Danger ? DangerColor : WarningColor);
    }

    private void OnToggleScreen(Entity<StationAiHeldComponent> ent, ref ToggleAiAlertsScreenEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(ent.Owner, out var actor))
            return;

        args.Handled = true;

        _ui.TryToggleUi(ent.Owner, AiAlertsUiKey.Key, actor.PlayerSession);
        PushState(ent.Owner);
    }

    private void OnRefresh(Entity<StationAiHeldComponent> ent, ref AiAlertsRefreshMessage args)
    {
        PushState(ent.Owner);
    }

    private void OnWarp(Entity<StationAiHeldComponent> ent, ref AiAlertWarpMessage args)
    {
        var netTarget = args.Target;
        var target = GetEntity(netTarget);
        if (Deleted(target))
            return;

        // linha que já não existe não teleporta, mas atualiza o monitor pra ela sumir
        // em vez de virar botão morto
        if (!BuildAlerts(ent.Owner).Any(alert => alert.Source == netTarget))
        {
            PushState(ent.Owner);
            return;
        }

        if (!_stationAi.TryGetCore(ent.Owner, out var core) || core.Comp?.RemoteEntity == null)
            return;

        if (_xform.GetGrid(core.Owner) != _xform.GetGrid(target))
            return;

        _xform.SetWorldPosition(core.Comp.RemoteEntity.Value, _xform.GetWorldPosition(target));

        // ir até lá conta como resolvido, a fonte fica de cooldown e volta se continuar ruim
        Suppress(ent.Owner, netTarget, _timing.CurTime + ResolveCooldown);
        PushState(ent.Owner);
    }

    private void OnDismiss(Entity<StationAiHeldComponent> ent, ref AiAlertDismissMessage args)
    {
        // na linha do ponto o botão é Remover e apaga o holograma de verdade
        var target = GetEntity(args.Target);
        if (_waypoints.IsWaypoint(ent.Owner, target))
        {
            _waypoints.RemoveWaypoint(ent.Owner, ent.Owner);
            PushState(ent.Owner);
            return;
        }

        Suppress(ent.Owner, args.Target, null);
        PushState(ent.Owner);
    }

    /// <summary>
    /// esconde a linha da fonte pra essa IA. sem until é ocultar, o problema atual morre e
    /// só volta se disparar de novo. com until é resolvido, a fonte volta depois do cooldown
    /// se ainda estiver ruim. alarme oculta a área inteira que a linha representa, porta
    /// oculta todas as batidas daquela porta
    /// </summary>
    private void Suppress(EntityUid ai, NetEntity source, TimeSpan? until)
    {
        var now = _timing.CurTime;
        if (!_suppressed.TryGetValue(ai, out var map))
        {
            map = new Dictionary<NetEntity, Suppression>();
            _suppressed[ai] = map;
        }

        var instance = until == null ? now : (TimeSpan?) null;
        var suppression = new Suppression(instance, until);

        var target = GetEntity(source);

        if (_active.TryGetValue(target, out var alarm))
        {
            foreach (var (uid, other) in _active)
            {
                if (other.Area == alarm.Area && other.Kind == alarm.Kind)
                    map[GetNetEntity(uid)] = suppression;
            }

            return;
        }

        map[source] = suppression;
    }

    private void PushState(EntityUid ai)
    {
        _ui.SetUiState(ai, AiAlertsUiKey.Key, new AiAlertsBuiState(BuildAlerts(ai)));
    }

    private void RefreshOpenMonitors()
    {
        var query = EntityQueryEnumerator<StationAiHeldComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (_ui.IsUiOpen(uid, AiAlertsUiKey.Key))
                PushState(uid);
        }
    }

    /// <summary>
    /// linhas vivas dessa IA: descarta o deletado, o vencido, o ocultado e o resolvido
    /// </summary>
    private List<AiAlertEntry> BuildAlerts(EntityUid ai)
    {
        var now = _timing.CurTime;
        var station = _station.GetOwningStation(ai);
        _suppressed.TryGetValue(ai, out var suppressed);

        var candidates = new List<(AiAlertEntry Entry, TimeSpan Instance)>();
        CollectAlarms(station, candidates);
        CollectKnocks(station, candidates);
        CollectWaypoint(ai, candidates);

        var alerts = new List<AiAlertEntry>();
        var byArea = new Dictionary<(string Area, AiAlertKind Kind), int>();

        foreach (var (entry, instance) in candidates)
        {
            if (now > instance + AlertLifetime)
                continue;

            if (suppressed != null && suppressed.TryGetValue(entry.Source, out var sup))
            {
                if (sup.Until is { } cooldownUntil && now < cooldownUntil)
                    continue;

                if (sup.Instance is { } hiddenUpTo && instance <= hiddenUpTo)
                    continue;

                // disparou de novo depois de ocultado, volta pra fila
                suppressed.Remove(entry.Source);
            }

            // alarmes da mesma área viram uma linha só, com a pior gravidade
            if (entry.Kind is AiAlertKind.Atmos or AiAlertKind.Fire)
            {
                var key = (entry.Area, entry.Kind);
                if (byArea.TryGetValue(key, out var index))
                {
                    if (entry.Severity > alerts[index].Severity)
                        alerts[index] = entry;

                    continue;
                }

                byArea[key] = alerts.Count;
            }

            alerts.Add(entry);
        }

        alerts.Sort((a, b) => a.Severity != b.Severity
            ? b.Severity.CompareTo(a.Severity)
            : string.Compare(a.Area, b.Area, StringComparison.CurrentCulture));

        return alerts;
    }

    private void CollectAlarms(EntityUid? station, List<(AiAlertEntry, TimeSpan)> candidates)
    {
        var stale = new List<EntityUid>();

        foreach (var (uid, alert) in _active)
        {
            if (Deleted(uid))
            {
                stale.Add(uid);
                continue;
            }

            if (station != null && alert.Station != null && alert.Station != station)
                continue;

            candidates.Add((new AiAlertEntry
            {
                Source = GetNetEntity(uid),
                Area = alert.Area,
                Kind = alert.Kind,
                Severity = alert.Severity,
            }, alert.At));
        }

        foreach (var uid in stale)
        {
            _active.Remove(uid);
        }
    }

    private void CollectKnocks(EntityUid? station, List<(AiAlertEntry, TimeSpan)> candidates)
    {
        var now = _timing.CurTime;
        var stale = new List<(EntityUid, EntityUid)>();

        foreach (var (key, knock) in _knocks)
        {
            if (now > knock.At + KnockLifetime || Deleted(key.Door))
            {
                stale.Add(key);
                continue;
            }

            if (station != null && knock.Station != null && knock.Station != station)
                continue;

            candidates.Add((new AiAlertEntry
            {
                Source = GetNetEntity(key.Door),
                Area = knock.Area,
                Subject = knock.Who,
                Kind = AiAlertKind.Door,
                Severity = AiAlertSeverity.Info,
            }, knock.FirstAt));
        }

        foreach (var key in stale)
        {
            _knocks.Remove(key);
        }
    }

    /// <summary>
    /// o ponto da própria IA pra ela voltar pro que marcou pros borgs
    /// </summary>
    private void CollectWaypoint(EntityUid ai, List<(AiAlertEntry, TimeSpan)> candidates)
    {
        if (!_waypoints.TryGetWaypoint(ai, out var marker, out var area, out var at))
            return;

        candidates.Add((new AiAlertEntry
        {
            Source = GetNetEntity(marker),
            Area = area,
            Kind = AiAlertKind.Waypoint,
            Severity = AiAlertSeverity.Info,
        }, at));
    }

    /// <summary>
    /// toda IA que está vigiando <paramref name="station"/> agora.
    /// </summary>
    private Filter GetAiFilter(EntityUid? station)
    {
        var filter = Filter.Empty();
        var query = EntityQueryEnumerator<StationAiHeldComponent, ActorComponent>();

        while (query.MoveNext(out var uid, out _, out var actor))
        {
            var aiStation = _station.GetOwningStation(uid);
            if (station != null && aiStation != null && aiStation != station)
                continue;

            filter.AddPlayer(actor.PlayerSession);
        }

        return filter;
    }
}
