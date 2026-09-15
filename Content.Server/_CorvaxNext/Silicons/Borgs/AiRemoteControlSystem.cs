// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 ImHoks <142083149+ImHoks@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 ImHoks <imhokzzzz@gmail.com>
// SPDX-FileCopyrightText: 2025 KillanGenifer <killangenifer@gmail.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Radio.Components;
using Content.Server.Silicons.Laws;
using Content.Shared._CorvaxNext.Silicons.Borgs;
using Content.Shared._CorvaxNext.Silicons.Borgs.Components;
using Content.Shared.Actions;
using Content.Shared.Mind;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationAi;
using Content.Shared.Verbs;
using Content.Shared.Radio; // Dumont-Boris-Tags
using Content.Shared.Silicons.Borgs; // Dumont-Boris-Tags
using Content.Shared.Silicons.Borgs.Components; // Dumont-Boris-Tags
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes; // Dumont-Boris-Tags

namespace Content.Server._CorvaxNext.Silicons.Borgs;

public sealed class AiRemoteControlSystem : SharedAiRemoteControlSystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SiliconLawSystem _lawSystem = default!;
    [Dependency] private readonly SharedStationAiSystem _stationAiSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!; // Dumont-Boris-Tags

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AiRemoteControllerComponent, ReturnMindIntoAiEvent>(OnReturnMindIntoAi);
        SubscribeLocalEvent<AiRemoteControllerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AiRemoteControllerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AiRemoteControllerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<StationAiHeldComponent, AiRemoteControllerComponent.RemoteDeviceActionMessage>(OnUiRemoteAction);
        SubscribeLocalEvent<StationAiHeldComponent, ToggleRemoteDevicesScreenEvent>(OnToggleRemoteDevicesScreen);
    }

    private void OnMapInit(Entity<AiRemoteControllerComponent> entity, ref MapInitEvent args)
    {
        var visionComp = EnsureComp<StationAiVisionComponent>(entity.Owner);
        EntityUid? actionEnt = null;

        _actions.AddAction(entity.Owner, ref actionEnt, entity.Comp.BackToAiAction);

        if (actionEnt != null)
            entity.Comp.BackToAiActionEntity = actionEnt.Value;
    }

    private void OnShutdown(Entity<AiRemoteControllerComponent> entity, ref ComponentShutdown args)
    {
        _actions.RemoveAction(entity.Owner, entity.Comp.BackToAiActionEntity);

        var backArgs = new ReturnMindIntoAiEvent();
        backArgs.Performer = entity;

        if (TryComp(entity, out IntrinsicRadioTransmitterComponent? transmitter)
            && entity.Comp.PreviouslyTransmitterChannels != null)
            transmitter.Channels = [.. entity.Comp.PreviouslyTransmitterChannels];

        if (TryComp(entity, out ActiveRadioComponent? activeRadio)
            && entity.Comp.PreviouslyActiveRadioChannels != null)
            activeRadio.Channels = [.. entity.Comp.PreviouslyActiveRadioChannels];

        ReturnMindIntoAi(entity);
    }

    private void OnGetVerbs(Entity<AiRemoteControllerComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;

        if (!TryComp<StationAiHeldComponent>(user, out var stationAiHeldComp))
            return;

        var isOccupied = entity.Comp.LinkedMind is not null; // Gabystation - ai fix

        var verb = new AlternativeVerb
        {
            Text = Loc.GetString("ai-remote-control"),
            Act = () => AiTakeControl(user, entity),
            Disabled = isOccupied, // Gabystation - ai fix
            Message = isOccupied ? Loc.GetString("ai-remote-control-occupied") : null // Gabystation - ai fix
        };
        args.Verbs.Add(verb);
    }

    private void OnReturnMindIntoAi(Entity<AiRemoteControllerComponent> entity, ref ReturnMindIntoAiEvent args) =>
        ReturnMindIntoAi(entity);

    public void AiTakeControl(EntityUid ai, EntityUid entity)
    {
        if (!_mind.TryGetMind(ai, out var mindId, out var mind))
            return;

        if (!TryComp<StationAiHeldComponent>(ai, out var stationAiHeldComp))
            return;

        if (!TryComp<AiRemoteControllerComponent>(entity, out var aiRemoteComp))
            return;

        if (TryComp(entity, out IntrinsicRadioTransmitterComponent? transmitter))
        {
            aiRemoteComp.PreviouslyTransmitterChannels = [.. transmitter.Channels];

            if (TryComp(ai, out IntrinsicRadioTransmitterComponent? stationAiTransmitter))
                transmitter.Channels = [.. stationAiTransmitter.Channels];
        }

        if (TryComp(entity, out ActiveRadioComponent? activeRadio))
        {
            aiRemoteComp.PreviouslyActiveRadioChannels = [.. activeRadio.Channels];

            if (TryComp(ai, out ActiveRadioComponent? stationAiActiveRadio))
                activeRadio.Channels = [.. stationAiActiveRadio.Channels];
        }

        _mind.ControlMob(ai, entity);
        aiRemoteComp.AiHolder = ai;
        aiRemoteComp.LinkedMind = mindId;

        stationAiHeldComp.CurrentConnectedEntity = entity;

        if (!_stationAiSystem.TryGetCore(ai, out var stationAiCore))
            return;

        _stationAiSystem.SwitchRemoteEntityMode(stationAiCore, false);

        RewriteLaws(ai, entity);
    }

    private void OnToggleRemoteDevicesScreen(EntityUid uid, StationAiHeldComponent component, ToggleRemoteDevicesScreenEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(uid, out var actor))
            return;
        args.Handled = true;

        _userInterface.TryToggleUi(uid, RemoteDeviceUiKey.Key, actor.PlayerSession);

        var query = EntityManager.EntityQueryEnumerator<AiRemoteControllerComponent>();
        var remoteDevices = new List<RemoteDevicesData>();

        while (query.MoveNext(out var queryUid, out var comp))
        {
            var isOccupied = comp.LinkedMind is not null; // Gabystation - ai fix

            // Dumont-Boris-Tags start: função do borg vira tag na lista, com a cor do rádio
            var tagText = string.Empty;
            var tagColor = Color.Transparent;
            var tagIsRadio = false;

            if (TryComp<BorgSwitchableTypeComponent>(queryUid, out var switchable)
                && switchable.SelectedBorgType is { } typeId
                && _proto.TryIndex(typeId, out var borgType))
            {
                // canal departamental ganha, o da ciência é o genérico que todo tipo tem
                ProtoId<RadioChannelPrototype>? pick = null;
                foreach (var channel in borgType.RadioChannels)
                {
                    if (channel == "Common" || channel == "Binary")
                        continue;

                    if (channel != "Science")
                    {
                        pick = channel;
                        break;
                    }

                    pick ??= channel;
                }

                if (pick is { } picked && _proto.TryIndex(picked, out var channelProto))
                {
                    tagText = Loc.GetString(channelProto.Name);
                    tagColor = channelProto.Color;
                    tagIsRadio = true;
                }
                else
                {
                    tagText = Loc.GetString($"borg-type-{typeId.Id}-name");
                }
            }
            // Dumont-Boris-Tags end

            var data = new RemoteDevicesData
            {
                NetEntityUid = GetNetEntity(queryUid),
                DisplayName = Comp<MetaDataComponent>(queryUid).EntityName, // Gabystation - ai fix
                IsOccupied = isOccupied, // Gabystation - ai fix
                TagText = tagText, // Dumont-Boris-Tags
                TagColor = tagColor, // Dumont-Boris-Tags
                TagIsRadio = tagIsRadio // Dumont-Boris-Tags
            };

            remoteDevices.Add(data);
        }

        var state = new RemoteDevicesBuiState(remoteDevices);
        _userInterface.SetUiState(uid, RemoteDeviceUiKey.Key, state);
    }

    private void OnUiRemoteAction(EntityUid uid, StationAiHeldComponent component, AiRemoteControllerComponent.RemoteDeviceActionMessage msg)
    {
        if (msg.RemoteAction == null)
            return;

        var target = GetEntity(msg.RemoteAction?.Target);

        if (!TryComp<AiRemoteControllerComponent>(target, out var remote))
            return;

        switch (msg.RemoteAction?.ActionType)
        {
            case RemoteDeviceActionEvent.RemoteDeviceActionType.MoveToDevice:
                if (!_stationAiSystem.TryGetCore(uid, out var stationAiCore)
                    || stationAiCore.Comp?.RemoteEntity == null)
                    return;
                _xformSystem.SetCoordinates(stationAiCore.Comp.RemoteEntity.Value, Transform(target.Value).Coordinates);
                break;

            case RemoteDeviceActionEvent.RemoteDeviceActionType.TakeControl:
                if (remote.LinkedMind is not null)
                    return;
                AiTakeControl(uid, target.Value);
                break;
        }
    }

    private void RewriteLaws(EntityUid from, EntityUid to)
    {
        if (!TryComp<SiliconLawProviderComponent>(from, out var fromLawsComp))
            return;

        if (!TryComp<SiliconLawProviderComponent>(to, out var toLawsComp))
            return;

        if (fromLawsComp.Lawset == null)
            return;

        var fromLaws = _lawSystem.GetLaws(from);
        _lawSystem.SetLawsSilent(fromLaws.Laws, to);
    }
}
