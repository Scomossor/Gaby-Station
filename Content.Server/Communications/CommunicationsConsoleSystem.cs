using Content.Server.Administration.Logs;
using Content.Server.AlertLevel;
using Content.Server.Chat.Systems;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.RoundEnd;
using Content.Server.Screens.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Communications;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.Emag.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Content.Shared.Doors.Components; //importa as portas iradas
using Content.Server.Doors.Systems; //importa o sistema das airlock pra botar o modo de emergencia
using System.Collections; //parao array de prototypes
using Robust.Shared.Audio;
using Content.Server.Administration;
using Robust.Shared.Player;
using Content.Server.Chat.Managers; //pra falar com centcom
using Robust.Shared.Timing; // para cooldown

namespace Content.Server.Communications
{
    public sealed class CommunicationsConsoleSystem : EntitySystem
    {
        [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
        [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
        [Dependency] private readonly EmergencyShuttleSystem _emergency = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly AirlockSystem _airlock = default!;
        [Dependency] private readonly QuickDialogSystem _quickDialog = default!; //cria dependencia na mensagem de popup igual eu tenho com a -----------
        [Dependency] private readonly IChatManager _chatManager = default!; // avbiso admin
        [Dependency] private readonly IGameTiming _timing = default!; // cooldown

        private const float UIUpdateInterval = 5.0f;

        // array dos prototypes que vao ficar em manutenção
        private ArrayList _maintDoorPrototypeList = new ArrayList();

        public override void Initialize()
        {
            // All events that refresh the BUI
            SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
            SubscribeLocalEvent<CommunicationsConsoleComponent, ComponentInit>((uid, comp, _) => UpdateCommsConsoleInterface(uid, comp));
            SubscribeLocalEvent<RoundEndSystemChangedEvent>(_ => OnGenericBroadcastEvent());
            SubscribeLocalEvent<AlertLevelDelayFinishedEvent>(_ => OnGenericBroadcastEvent());

            // Messages from the BUI
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleSelectAlertLevelMessage>(OnSelectAlertLevelMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleAnnounceMessage>(OnAnnounceMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleBroadcastMessage>(OnBroadcastMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleCallEmergencyShuttleMessage>(OnCallShuttleMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleRecallEmergencyShuttleMessage>(OnRecallShuttleMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleToggleEmergencyMaintMessage>(OnToggleEmergencyMaintMessage);
            SubscribeLocalEvent<CommunicationsConsoleComponent, CommunicationsConsoleCentCommButtonMessage>(OnCentCommMessage);

            // On console init, set cooldown
            SubscribeLocalEvent<CommunicationsConsoleComponent, MapInitEvent>(OnCommunicationsConsoleMapInit);

            // adicione os prototypes das portas que vao entrar em modo de emergencia aqui:
            _maintDoorPrototypeList.Add("AirlockMaintGlassLocked");
            _maintDoorPrototypeList.Add("AirlockMaintLocked");
            _maintDoorPrototypeList.Add("AirlockMaintCommonLocked");
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<CommunicationsConsoleComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                // TODO refresh the UI in a less horrible way
                if (comp.AnnouncementCooldownRemaining >= 0f)
                {
                    comp.AnnouncementCooldownRemaining -= frameTime;
                }

                comp.UIUpdateAccumulator += frameTime;

                if (comp.UIUpdateAccumulator < UIUpdateInterval)
                    continue;

                comp.UIUpdateAccumulator -= UIUpdateInterval;

                if (_uiSystem.IsUiOpen(uid, CommunicationsConsoleUiKey.Key))
                    UpdateCommsConsoleInterface(uid, comp);
            }

            base.Update(frameTime);
        }

        public void OnCommunicationsConsoleMapInit(EntityUid uid, CommunicationsConsoleComponent comp, MapInitEvent args)
        {
            comp.AnnouncementCooldownRemaining = comp.InitialDelay;
        }

        /// <summary>
        /// Update the UI of every comms console.
        /// </summary>
        private void OnGenericBroadcastEvent()
        {
            var query = EntityQueryEnumerator<CommunicationsConsoleComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                UpdateCommsConsoleInterface(uid, comp);
            }
        }

        /// <summary>
        /// Updates all comms consoles belonging to the station that the alert level was set on
        /// </summary>
        /// <param name="args">Alert level changed event arguments</param>
        private void OnAlertLevelChanged(AlertLevelChangedEvent args)
        {
            var query = EntityQueryEnumerator<CommunicationsConsoleComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                var entStation = _stationSystem.GetOwningStation(uid);
                if (args.Station == entStation)
                    UpdateCommsConsoleInterface(uid, comp);
            }
        }

        /// <summary>
        /// Updates the UI for all comms consoles.
        /// </summary>
        public void UpdateCommsConsoleInterface()
        {
            var query = EntityQueryEnumerator<CommunicationsConsoleComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                UpdateCommsConsoleInterface(uid, comp);
            }
        }

        /// <summary>
        /// Updates the UI for a particular comms console.
        /// </summary>
        public void UpdateCommsConsoleInterface(EntityUid uid, CommunicationsConsoleComponent comp)
        {
            var stationUid = _stationSystem.GetOwningStation(uid);
            List<string>? levels = null;
            string currentLevel = default!;
            float currentDelay = 0;

            if (stationUid != null)
            {
                if (TryComp(stationUid.Value, out AlertLevelComponent? alertComp) &&
                    alertComp.AlertLevels != null)
                {
                    if (alertComp.IsSelectable)
                    {
                        levels = new();
                        foreach (var (id, detail) in alertComp.AlertLevels.Levels)
                        {
                            if (detail.Selectable)
                            {
                                levels.Add(id);
                            }
                        }
                    }

                    currentLevel = alertComp.CurrentLevel;
                    currentDelay = _alertLevelSystem.GetAlertLevelDelay(stationUid.Value, alertComp);
                }
            }

            _uiSystem.SetUiState(uid, CommunicationsConsoleUiKey.Key, new CommunicationsConsoleInterfaceState(
                CanAnnounce(comp),
                CanCallOrRecall(comp),
                levels,
                currentLevel,
                currentDelay,
                _roundEndSystem.ExpectedCountdownEnd
            ));
        }

        private static bool CanAnnounce(CommunicationsConsoleComponent comp)
        {
            return comp.AnnouncementCooldownRemaining <= 0f;
        }

        private bool CanUse(EntityUid user, EntityUid console)
        {
            if (TryComp<AccessReaderComponent>(console, out var accessReaderComponent) && !HasComp<EmaggedComponent>(console))
            {
                return _accessReaderSystem.IsAllowed(user, console, accessReaderComponent);
            }
            return true;
        }

        private bool CanCallOrRecall(CommunicationsConsoleComponent comp)
        {
            // Defer to what the round end system thinks we should be able to do.
            if (_emergency.EmergencyShuttleArrived || !_roundEndSystem.CanCallOrRecall())
                return false;

            // Ensure that we can communicate with the shuttle (either call or recall)
            if (!comp.CanShuttle)
                return false;

            // Calling shuttle checks
            if (_roundEndSystem.ExpectedCountdownEnd is null)
                return true;

            // Recalling shuttle checks
            var recallThreshold = _cfg.GetCVar(CCVars.EmergencyRecallTurningPoint);

            // shouldn't really be happening if we got here
            if (_roundEndSystem.ShuttleTimeLeft is not { } left
                || _roundEndSystem.ExpectedShuttleLength is not { } expected)
                return false;

            return !(left.TotalSeconds / expected.TotalSeconds < recallThreshold);
        }

        private void OnSelectAlertLevelMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleSelectAlertLevelMessage message)
        {
            if (message.Actor is not { Valid: true } mob)
                return;

            if (!CanUse(mob, uid))
            {
                _popupSystem.PopupCursor(Loc.GetString("comms-console-permission-denied"), message.Actor, PopupType.Medium);
                return;
            }

            var stationUid = _stationSystem.GetOwningStation(uid);
            if (stationUid != null)
            {
                _alertLevelSystem.SetLevel(stationUid.Value, message.Level, true, true);
            }
        }

        private void OnAnnounceMessage(EntityUid uid, CommunicationsConsoleComponent comp,
            CommunicationsConsoleAnnounceMessage message)
        {
            var maxLength = _cfg.GetCVar(CCVars.ChatMaxAnnouncementLength);
            var msg = SharedChatSystem.SanitizeAnnouncement(message.Message, maxLength);
            var author = Loc.GetString("comms-console-announcement-unknown-sender");
            if (message.Actor is { Valid: true } mob)
            {
                if (!CanAnnounce(comp))
                {
                    return;
                }

                if (!CanUse(mob, uid))
                {
                    _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Actor);
                    return;
                }

                var tryGetIdentityShortInfoEvent = new TryGetIdentityShortInfoEvent(uid, mob);
                RaiseLocalEvent(tryGetIdentityShortInfoEvent);
                author = tryGetIdentityShortInfoEvent.Title;
            }

            comp.AnnouncementCooldownRemaining = comp.Delay;
            UpdateCommsConsoleInterface(uid, comp);

            var ev = new CommunicationConsoleAnnouncementEvent(uid, comp, msg, message.Actor);
            RaiseLocalEvent(ref ev);

            // allow admemes with vv
            Loc.TryGetString(comp.Title, out var title);
            title ??= comp.Title;

            msg += "\n" + Loc.GetString("comms-console-announcement-sent-by") + " " + author;
            if (comp.Global)
            {
                _chatSystem.DispatchGlobalAnnouncement(msg, title, announcementSound: comp.Sound, colorOverride: comp.Color);

                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"{ToPrettyString(message.Actor):player} has sent the following global announcement: {msg}");
                return;
            }

            _chatSystem.DispatchStationAnnouncement(uid, msg, title, colorOverride: comp.Color);

            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"{ToPrettyString(message.Actor):player} has sent the following station announcement: {msg}");

        }

        private void OnBroadcastMessage(EntityUid uid, CommunicationsConsoleComponent component, CommunicationsConsoleBroadcastMessage message)
        {
            if (!TryComp<DeviceNetworkComponent>(uid, out var net))
                return;

            var payload = new NetworkPayload
            {
                [ScreenMasks.Text] = message.Message
            };

            _deviceNetworkSystem.QueuePacket(uid, null, payload, net.TransmitFrequency);

            _adminLogger.Add(LogType.DeviceNetwork, LogImpact.Low, $"{ToPrettyString(message.Actor):player} has sent the following broadcast: {message.Message:msg}");
        }

        private void OnCentCommMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleCentCommButtonMessage message)
        {
            if (!EntityManager.TryGetComponent(message.Actor, out ActorComponent? actor))
                return;

            var mob = message.Actor;
            if (!CanUse(mob, uid))
            {
                _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Actor);
                return;
            }
            //dialogo
            _quickDialog.OpenDialog(actor.PlayerSession, Loc.GetString("comms-console-menu-dialog-centcom-tittle"), Loc.GetString("comms-console-menu-dialog-centcom-message"), (string centMessage) =>
            {
                if (!centMessage.Equals("")) //se nao tiver vazio
                {
                    _chatManager.SendAdminAnnouncement($"{ToPrettyString(mob):player}: Enviou mensagem para CENTCOM '{centMessage}'"); // mensagem de admin (muito uim usar pray)
                    _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(mob):player} has sent a message to centcom, message: '{centMessage}'."); //log
                    _popupSystem.PopupEntity(Loc.GetString("comns-console-centcom-send"), uid, message.Actor);
                    return;
                } //pop up avisando q ta vazio
                _popupSystem.PopupEntity(Loc.GetString("comns-console-empty-input"), uid, message.Actor);
            });
        }

        //função de alterar acesso de emergencia
        private void OnToggleEmergencyMaintMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleToggleEmergencyMaintMessage message)
        {
            if ((_timing.CurTime.TotalSeconds - comp.ToggleAcessTimer) < comp.ToggleAcessDelay) //coldown
                return;

            var mob = message.Actor;
            if (!CanUse(mob, uid))
            {
                _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Actor);
                return;
            }
            _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(mob):player} has toggle the station maintance access."); //bota log de admin em ingles por que sou muito estadunidense slk

            _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("comms-console-announcement-content-maint"), Loc.GetString("comms-console-announcement-title-station"), announcementSound: comp.Sound, colorOverride: comp.Color);

            // itera as portas DO PROTOTYPE de maint da estação
            var query = EntityQueryEnumerator<DoorComponent>();
            while (query.MoveNext(out var doorUid, out var component))
            {
                if (!TryGetNetEntity(doorUid, out var netEntity)
                    || !TryGetEntityData(netEntity.Value, out var entityUid, out var entityData)
                    || !_maintDoorPrototypeList.Contains(entityData.EntityPrototype!.ID)
                    || !TryComp<AirlockComponent>(doorUid, out var airlock))
                    continue;

                var ent = new Entity<AirlockComponent>(doorUid, airlock);

                _airlock.SetEmergencyAccess(ent, !airlock.EmergencyAccess);
            }
            comp.ToggleAcessTimer = _timing.CurTime.TotalSeconds;
        }
        private void OnCallShuttleMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleCallEmergencyShuttleMessage message)
        {
            if (!EntityManager.TryGetComponent(message.Actor, out ActorComponent? actor)
                || !CanCallOrRecall(comp))
                return;

            var mob = message.Actor;

            if (!CanUse(mob, uid))
            {
                _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Actor);
                return;
            }

            var ev = new CommunicationConsoleCallShuttleAttemptEvent(uid, comp, mob);
            RaiseLocalEvent(ref ev);
            if (ev.Cancelled)
            {
                _popupSystem.PopupEntity(ev.Reason ?? Loc.GetString("comms-console-shuttle-unavailable"), uid, message.Actor);
                return;
            }

            // dialogo
            _quickDialog.OpenDialog(actor.PlayerSession, Loc.GetString("comms-console-menu-dialog-shuttle-tittle"), Loc.GetString("comms-console-menu-dialog-shuttle-message"), (string reason) =>
            {
                if (!reason.Equals(""))
                {
                    _roundEndSystem.RequestRoundEnd(uid, text: "round-end-system-shuttle-called-announcement-with-reason", name: "comms-console-announcement-title-station", hasReason: true, reason: reason);
                    _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(mob):player} has called the shuttle with reason '{reason}'.");
                    return;
                }
                _roundEndSystem.RequestRoundEnd(uid, name: "comms-console-announcement-title-station");
                _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(mob):player} has called the shuttle.");
            });
        }

        private void OnRecallShuttleMessage(EntityUid uid, CommunicationsConsoleComponent comp, CommunicationsConsoleRecallEmergencyShuttleMessage message)
        {
            if (!CanCallOrRecall(comp))
                return;

            if (!CanUse(message.Actor, uid))
            {
                _popupSystem.PopupEntity(Loc.GetString("comms-console-permission-denied"), uid, message.Actor);
                return;
            }

            _roundEndSystem.CancelRoundEndCountdown(uid, name: "comms-console-announcement-title-station");
            _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(message.Actor):player} has recalled the shuttle.");
        }
    }



    /// <summary>
    /// Raised on announcement
    /// </summary>
    [ByRefEvent]
    public record struct CommunicationConsoleAnnouncementEvent(EntityUid Uid, CommunicationsConsoleComponent Component, string Text, EntityUid? Sender)
    {
        public EntityUid Uid = Uid;
        public CommunicationsConsoleComponent Component = Component;
        public EntityUid? Sender = Sender;
        public string Text = Text;
    }

    /// <summary>
    /// Raised on shuttle call attempt. Can be cancelled
    /// </summary>
    [ByRefEvent]
    public record struct CommunicationConsoleCallShuttleAttemptEvent(EntityUid Uid, CommunicationsConsoleComponent Component, EntityUid? Sender)
    {
        public bool Cancelled = false;
        public EntityUid Uid = Uid;
        public CommunicationsConsoleComponent Component = Component;
        public EntityUid? Sender = Sender;
        public string? Reason;
    }
}
