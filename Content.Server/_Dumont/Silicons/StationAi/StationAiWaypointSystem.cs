// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Pinpointer;
using Content.Shared._Dumont.Silicons.StationAi;
using Content.Shared._Starlight.CollectiveMind;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Silicons.StationAi;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Dumont.Silicons.StationAi;

/// <summary>
/// deixa a IA marcar um ponto pro coletivo silicon
/// o binário já carregava "vai pro cargo", mas nada amarrava a mensagem a um lugar. o ponto
/// nomeia o local pelo beacon mais próximo e deixa uma âncora pra IA voltar
/// o marcador é um holograma visível no mundo, projetado pela IA. quem passar por ele vê,
/// o que é aceitável.. holograma de IA marcando lugar é fluff válido, não vazamento
/// </summary>
public sealed class StationAiWaypointSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _xform = default!;

    /// <summary>
    /// canal que o coletivo silicon já divide.
    /// </summary>
    private const string BinaryMind = "Binary";

    private const string MarkerProto = "AiWaypointMarker";

    /// <summary>
    /// ponto velho é pior que ponto nenhum então eles vencem sozinhos
    /// </summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    /// <summary>
    /// um marcador por IA. marcar de novo move ele em vez de empilhar
    /// </summary>
    private readonly Dictionary<EntityUid, (EntityUid Marker, string Area, TimeSpan At)> _waypoints = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAiHeldComponent, AiPlaceWaypointEvent>(OnPlaceWaypoint);
        SubscribeLocalEvent<AiWaypointMarkerComponent, AiWaypointRemoveEvent>(OnMarkerRemove);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        foreach (var (_, waypoint) in _waypoints)
        {
            QueueDel(waypoint.Marker);
        }

        _waypoints.Clear();
    }

    private void OnPlaceWaypoint(Entity<StationAiHeldComponent> ent, ref AiPlaceWaypointEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        // marcar em cima do próprio ponto remove em vez de mover, vira toggle
        if (_waypoints.TryGetValue(ent.Owner, out var existing) && !Deleted(existing.Marker))
        {
            var markerCoords = Transform(existing.Marker).Coordinates;
            if (args.Target.TryDistance(EntityManager, markerCoords, out var dist) && dist < 1f)
            {
                RemoveWaypoint(ent.Owner, ent.Owner);
                return;
            }
        }

        Clear(ent.Owner);

        var marker = Spawn(MarkerProto, args.Target);
        var markerComp = EnsureComp<AiWaypointMarkerComponent>(marker);
        markerComp.Ai = ent.Owner;
        Dirty(marker, markerComp);
        var area = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString((marker, null)));

        _waypoints[ent.Owner] = (marker, area, _timing.CurTime);

        // feedback imediato pra quem marcou, o anúncio no binário é pros outros
        _popup.PopupEntity(Loc.GetString("station-ai-waypoint-set", ("area", area)), marker, ent.Owner);

        Announce(ent.Owner, Loc.GetString("station-ai-waypoint-set", ("area", area)));
    }

    /// <summary>
    /// o ponto vivo da IA, se ela tem um e ele não venceu.
    /// </summary>
    public bool TryGetWaypoint(EntityUid ai, out EntityUid marker, out string area, out TimeSpan at)
    {
        marker = default;
        area = string.Empty;
        at = default;

        if (!_waypoints.TryGetValue(ai, out var waypoint))
            return false;

        if (Deleted(waypoint.Marker) || _timing.CurTime > waypoint.At + Lifetime)
        {
            Clear(ai);
            return false;
        }

        marker = waypoint.Marker;
        area = waypoint.Area;
        at = waypoint.At;
        return true;
    }

    /// <summary>
    /// escolheu remover no radial do holograma
    /// </summary>
    private void OnMarkerRemove(Entity<AiWaypointMarkerComponent> ent, ref AiWaypointRemoveEvent args)
    {
        if (ent.Comp.Ai is not { } ai || ai != args.User)
            return;

        RemoveWaypoint(ai, args.User);
    }

    /// <summary>
    /// apaga o ponto de verdade, com feedback pra quem removeu e aviso no binário
    /// </summary>
    public void RemoveWaypoint(EntityUid ai, EntityUid? feedbackTo = null)
    {
        if (!_waypoints.TryGetValue(ai, out var waypoint))
            return;

        var message = Loc.GetString("station-ai-waypoint-removed", ("area", waypoint.Area));

        if (feedbackTo != null && !Deleted(waypoint.Marker))
            _popup.PopupEntity(message, waypoint.Marker, feedbackTo.Value);

        Clear(ai);
        Announce(ai, message);
    }

    public bool IsWaypoint(EntityUid ai, EntityUid marker)
    {
        return _waypoints.TryGetValue(ai, out var waypoint) && waypoint.Marker == marker;
    }

    private void Clear(EntityUid ai)
    {
        if (!_waypoints.Remove(ai, out var waypoint))
            return;

        QueueDel(waypoint.Marker);
    }

    /// <summary>
    /// fala no binário, montado na mão em vez de usar o ChatSystem porque o caminho de envio da
    /// mente coletiva é privado e espera um jogador de verdade falando
    /// </summary>
    private void Announce(EntityUid source, string message)
    {
        if (!_proto.TryIndex<CollectiveMindPrototype>(BinaryMind, out var mind))
            return;

        var clients = Filter.Empty();
        var query = EntityQueryEnumerator<CollectiveMindComponent, ActorComponent>();
        while (query.MoveNext(out var uid, out var collective, out var actor))
        {
            if (_mobState.IsDead(uid))
                continue;

            // Channels é a configuração fixa do canal. o dicionário Minds só enche quando
            // a entidade fala no coletivo, então num round quieto ele filtra todo mundo
            if (collective.Channels.Contains(BinaryMind) || collective.HearAll)
                clients.AddPlayer(actor.PlayerSession);
        }

        if (clients.Count == 0)
            return;

        var wrapped = Loc.GetString("station-ai-waypoint-wrap",
            ("channel", mind.LocalizedName),
            ("message", message));

        _chatManager.ChatMessageToManyFiltered(clients,
            ChatChannel.CollectiveMind,
            message,
            wrapped,
            source,
            false,
            true,
            mind.Color);
    }
}
