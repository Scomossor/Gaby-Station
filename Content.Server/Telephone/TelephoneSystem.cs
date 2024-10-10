using Content.Server.Access.Systems;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.Interaction;
using Content.Server.Power.EntitySystems;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Power;
using Content.Shared.Speech;
using Content.Shared.Telephone;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Replays;
using System.Linq;
using Robust.Shared.GameObjects;

namespace Content.Server.Telephone;

public sealed class TelephoneSystem : SharedTelephoneSystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly IdCardSystem _idCardSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IReplayRecordingManager _replay = default!;

    // Has set used to prevent telephone feedback loops
    private HashSet<(EntityUid, string, Entity<TelephoneComponent>)> _recentChatMessages = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TelephoneComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<TelephoneComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<TelephoneComponent, ListenAttemptEvent>(OnAttemptListen);
        SubscribeLocalEvent<TelephoneComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<TelephoneComponent, TelephoneMessageReceivedEvent>(OnTelephoneMessageReceived);
    }

    #region: Events

    private void OnComponentShutdown(Entity<TelephoneComponent> entity, ref ComponentShutdown ev)
    {
        TerminateTelephoneCalls(entity);
    }

    private void OnPowerChanged(Entity<TelephoneComponent> entity, ref PowerChangedEvent ev)
    {
        if (!ev.Powered)
            TerminateTelephoneCalls(entity);
    }

    private void OnAttemptListen(Entity<TelephoneComponent> entity, ref ListenAttemptEvent args)
    {
        if (!this.IsPowered(entity, EntityManager)
            || !_interaction.InRangeUnobstructed(args.Source, entity.Owner, 0))
        {
            args.Cancel();
        }
    }

    private void OnListen(Entity<TelephoneComponent> entity, ref ListenEvent args)
    {
        if (args.Source == entity.Owner)
            return;

        // Ignore background chatter from non-player entities
        if (!HasComp<MindContainerComponent>(args.Source))
            return;

        // Simple check to make sure that we haven't sent this message already this frame
        if (_recentChatMessages.Add((args.Source, args.Message, entity)))
            SendTelephoneMessage(args.Source, args.Message, entity);
    }

    private void OnTelephoneMessageReceived(Entity<TelephoneComponent> entity, ref TelephoneMessageReceivedEvent args)
    {
        // Prevent message feedback loops
        if (entity == args.TelephoneSource)
            return;

        if (!this.IsPowered(entity, EntityManager))
            return;

        var nameEv = new TransformSpeakerNameEvent(args.MessageSource, Name(args.MessageSource));
        RaiseLocalEvent(args.MessageSource, nameEv);

        var name = Loc.GetString("speech-name-relay",
            ("speaker", Name(entity)),
            ("originalName", nameEv.VoiceName));

        var volume = entity.Comp.SpeakerVolume == TelephoneVolume.Speak ? InGameICChatType.Speak : InGameICChatType.Whisper;
        _chat.TrySendInGameICMessage(entity, args.Message, volume, ChatTransmitRange.GhostRangeLimit, nameOverride: name, checkRadioPrefix: false);
    }

    #endregion

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityManager.EntityQueryEnumerator<TelephoneComponent>();
        while (query.MoveNext(out var uid, out var telephone))
        {
            var entity = new Entity<TelephoneComponent>(uid, telephone);

            switch (telephone.CurrentState)
            {
                // Try to play ring tone if ringing
                case TelephoneState.Ringing:
                    if (_timing.RealTime > telephone.StateStartTime + TimeSpan.FromSeconds(telephone.RingingTimeout))
                        EndTelephoneCalls(entity);

                    else if (telephone.RingTone != null &&
                        _timing.RealTime > telephone.NextRingToneTime)
                    {
                        _audio.PlayPvs(telephone.RingTone, uid);
                        telephone.NextRingToneTime = _timing.RealTime + TimeSpan.FromSeconds(telephone.RingInterval);
                    }

                    break;

                // Try to terminate if the telephone has finished hanging up
                case TelephoneState.EndingCall:
                    if (_timing.RealTime > telephone.StateStartTime + TimeSpan.FromSeconds(telephone.HangingUpTimeout))
                        TerminateTelephoneCalls(entity);

                    break;
            }
        }

        _recentChatMessages.Clear();
    }

    public void BroadcastCallToTelephones(Entity<TelephoneComponent> source, HashSet<Entity<TelephoneComponent>> receivers, EntityUid user, bool muteReceivers = false)
    {
        if (IsTelephoneEngaged(source))
            return;

        var options = new TelephoneCallOptions()
        {
            ForceConnect = true,
            MuteReceiver = muteReceivers,
        };

        foreach (var receiver in receivers)
            TryCallTelephone(source, receiver, user, options);

        // If no connections could be made, hang up the telephone
        if (!IsTelephoneEngaged(source))
            EndTelephoneCalls(source);
    }

    public void CallTelephone(Entity<TelephoneComponent> source, Entity<TelephoneComponent> receiver, EntityUid user, TelephoneCallOptions? options = null)
    {
        if (IsTelephoneEngaged(source))
            return;

        if (!TryCallTelephone(source, receiver, user, options))
            EndTelephoneCalls(source);
    }

    private bool TryCallTelephone(Entity<TelephoneComponent> source, Entity<TelephoneComponent> receiver, EntityUid user, TelephoneCallOptions? options = null)
    {
        if (!IsSourceAbleToReachReceiver(source, receiver))
            return false;

        var evCallAttempt = new TelephoneCallAttemptEvent(source, receiver, user);
        RaiseLocalEvent(source, ref evCallAttempt);

        if (evCallAttempt.Cancelled)
            return false;

        if (options?.ForceConnect == true)
            TerminateTelephoneCalls(receiver);

        source.Comp.LinkedTelephones.Add(receiver);
        source.Comp.Muted = options?.MuteSource == true;

        receiver.Comp.LastCaller = user;
        receiver.Comp.LinkedTelephones.Add(source);
        receiver.Comp.Muted = options?.MuteReceiver == true;

        // Try to open a line of communication immediately
        if (options?.ForceConnect == true ||
            (options?.ForceJoin == true && receiver.Comp.CurrentState == TelephoneState.InCall))
        {
            CommenceTelephoneCall(source, receiver);
            return true;
        }

        // Otherwise start ringing the receiver
        SetTelephoneState(source, TelephoneState.Calling);
        SetTelephoneState(receiver, TelephoneState.Ringing);

        return true;
    }

    public void AnswerTelephone(Entity<TelephoneComponent> receiver, EntityUid user)
    {
        if (receiver.Comp.CurrentState != TelephoneState.Ringing)
            return;

        // If the telephone isn't linked, or is linked to more than one telephone,
        // you shouldn't need to answer the call. If you do need to answer it,
        // you'll need to be handled this a different way
        if (receiver.Comp.LinkedTelephones.Count != 1)
            return;

        var source = receiver.Comp.LinkedTelephones.First();
        CommenceTelephoneCall(source, receiver);
    }

    private void CommenceTelephoneCall(Entity<TelephoneComponent> source, Entity<TelephoneComponent> receiver)
    {
        SetTelephoneState(source, TelephoneState.InCall);
        SetTelephoneState(receiver, TelephoneState.InCall);

        SetTelephoneMicrophoneState(source, true);
        SetTelephoneMicrophoneState(receiver, true);

        var evSource = new TelephoneCallCommencedEvent(receiver);
        var evReceiver = new TelephoneCallCommencedEvent(source);

        RaiseLocalEvent(source, ref evSource);
        RaiseLocalEvent(receiver, ref evReceiver);
    }

    public void EndTelephoneCalls(Entity<TelephoneComponent> entity)
    {
        HandleEndingTelephoneCalls(entity, TelephoneState.EndingCall);

        var ev = new TelephoneCallEndedEvent();
        RaiseLocalEvent(entity, ref ev);
    }

    public void TerminateTelephoneCalls(Entity<TelephoneComponent> entity)
    {
        HandleEndingTelephoneCalls(entity, TelephoneState.Idle);
    }

    private void HandleEndingTelephoneCalls(Entity<TelephoneComponent> entity, TelephoneState newState)
    {
        if (entity.Comp.CurrentState == newState)
            return;

        foreach (var linkedTelephone in entity.Comp.LinkedTelephones)
        {
            if (!linkedTelephone.Comp.LinkedTelephones.Remove(entity))
                continue;

            if (!IsTelephoneEngaged(linkedTelephone))
                EndTelephoneCalls(linkedTelephone);
        }

        entity.Comp.LinkedTelephones.Clear();
        entity.Comp.Muted = false;

        SetTelephoneState(entity, newState);
        SetTelephoneMicrophoneState(entity, true);
    }

    public void SendTelephoneMessage(EntityUid messageSource, string message, Entity<TelephoneComponent> source, bool escapeMarkup = true)
    {
        if (source.Comp.Muted || !IsTelephoneEngaged(source) || !this.IsPowered(source, EntityManager))
            return;

        var ev = new TransformSpeakerNameEvent(messageSource, MetaData(messageSource).EntityName);
        RaiseLocalEvent(messageSource, ev);

        var name = ev.VoiceName;
        name = FormattedMessage.EscapeText(name);

        SpeechVerbPrototype speech;
        if (ev.SpeechVerb != null && _prototype.TryIndex(ev.SpeechVerb, out var evntProto))
            speech = evntProto;
        else
            speech = _chat.GetSpeechVerb(messageSource, message);

        var content = escapeMarkup
            ? FormattedMessage.EscapeText(message)
            : message;

        var wrappedMessage = Loc.GetString(speech.Bold ? "chat-telephone-message-wrap-bold" : "chat-telephone-message-wrap",
            ("color", Color.White),
            ("fontType", speech.FontId),
            ("fontSize", speech.FontSize),
            ("verb", Loc.GetString(_random.Pick(speech.SpeechVerbStrings))),
            ("name", name),
            ("message", content));

        var chat = new ChatMessage(
            ChatChannel.Local,
            message,
            wrappedMessage,
            NetEntity.Invalid,
            null);

        var chatMsg = new MsgChatMessage { Message = chat };

        var evSentMessage = new TelephoneMessageSentEvent(message, chatMsg, messageSource);
        RaiseLocalEvent(source, ref evSentMessage);

        var evReceivedMessage = new TelephoneMessageReceivedEvent(message, chatMsg, messageSource, source);

        foreach (var receiver in source.Comp.LinkedTelephones)
        {
            if (!IsSourceAbleToReachReceiver(source, receiver))
                continue;

            RaiseLocalEvent(receiver, ref evReceivedMessage);
        }

        if (name != Name(messageSource))
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Telephone message from {ToPrettyString(messageSource):user} as {name} on {source}: {message}");
        else
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Telephone message from {ToPrettyString(messageSource):user} on {source}: {message}");

        _replay.RecordServerMessage(chat);
    }

    private void SetTelephoneState(Entity<TelephoneComponent> entity, TelephoneState newState)
    {
        var oldState = entity.Comp.CurrentState;

        entity.Comp.CurrentState = newState;
        entity.Comp.StateStartTime = _timing.RealTime;
        Dirty(entity);

        _appearanceSystem.SetData(entity, TelephoneVisuals.Key, entity.Comp.CurrentState);

        var ev = new TelephoneStateChangeEvent(oldState, newState);
        RaiseLocalEvent(entity, ref ev);
    }

    private void SetTelephoneMicrophoneState(Entity<TelephoneComponent> entity, bool microphoneOn)
    {
        if (microphoneOn && !HasComp<ActiveListenerComponent>(entity))
        {
            var activeListener = AddComp<ActiveListenerComponent>(entity);
            activeListener.Range = entity.Comp.ListeningRange;
        }

        if (!microphoneOn && HasComp<ActiveListenerComponent>(entity))
        {
            RemComp<ActiveListenerComponent>(entity);
        }
    }

    public string? GetFormattedCallerIdForEntity(EntityUid uid, Color fontColor, string fontType = "Default", int fontSize = 12)
    {
        var callerId = Loc.GetString("chat-telephone-unknown-caller",
            ("color", fontColor),
            ("fontType", fontType),
            ("fontSize", fontSize));

        if (_idCardSystem.TryFindIdCard(uid, out var idCard))
        {
            var presumedName = string.IsNullOrWhiteSpace(idCard.Comp.FullName) ? null : idCard.Comp.FullName;
            var presumedJob = idCard.Comp.JobTitle?.ToLowerInvariant();

            if (presumedName != null)
            {
                if (presumedJob != null)
                    callerId = Loc.GetString("chat-telephone-caller-id-with-job",
                        ("callerName", presumedName),
                        ("callerJob", presumedJob),
                        ("color", fontColor),
                        ("fontType", fontType),
                        ("fontSize", fontSize));

                else
                    callerId = Loc.GetString("chat-telephone-caller-id-without-job",
                        ("callerName", presumedName),
                        ("color", fontColor),
                        ("fontType", fontType),
                        ("fontSize", fontSize));
            }
        }

        return callerId;
    }

    public bool IsSourceAbleToReachReceiver(Entity<TelephoneComponent> source, Entity<TelephoneComponent> receiver)
    {
        if (source == receiver ||
            !this.IsPowered(source, EntityManager) ||
            !this.IsPowered(receiver, EntityManager) ||
            !IsSourceInRangeOfReceiver(source, receiver))
        {
            return false;
        }

        return true;
    }

    public bool IsSourceInRangeOfReceiver(Entity<TelephoneComponent> source, Entity<TelephoneComponent> receiver)
    {
        var sourceXform = Transform(source);
        var receiverXform = Transform(receiver);

        switch (source.Comp.TransmissionRange)
        {
            case TelephoneRange.Grid:
                if (sourceXform.GridUid == null || receiverXform.GridUid != sourceXform.GridUid)
                    return false;
                break;

            case TelephoneRange.Map:
                if (sourceXform.MapID != receiverXform.MapID)
                    return false;
                break;

            case TelephoneRange.Long:
                if (sourceXform.MapID == receiverXform.MapID || receiver.Comp.TransmissionRange != TelephoneRange.Long)
                    return false;
                break;
        }

        return true;
    }

    public bool IsSourceConnectedToReceiver(Entity<TelephoneComponent> source, Entity<TelephoneComponent> receiver)
    {
        return source.Comp.LinkedTelephones.Contains(receiver);
    }
}