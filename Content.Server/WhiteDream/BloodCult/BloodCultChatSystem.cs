// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server._EinsteinEngines.Language;
using Content.Shared.Chat;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Content.Shared._EinsteinEngines.Language;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.WhiteDream.BloodCult;

public sealed partial class BloodCultChatSystem : EntitySystem
{
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private IChatManager _chatManager = default!;

    [Dependency] private LanguageSystem _language = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultistComponent, EntitySpokeEvent>(OnSpeak);
    }

    private void OnSpeak(EntityUid uid, BloodCultistComponent component, EntitySpokeEvent args)
    {
        if (args.Source != uid || args.Language.ID != component.CultLanguageId || args.IsWhisper)
            return;

        SendMessage(args.Source, args.Message, false, args.Language);
    }

    private void SendMessage(EntityUid source, string message, bool hideChat, LanguagePrototype language)
    {
        var clients = GetClients(language.ID);
        var playerName = Name(source);
        var wrappedMessage = Loc.GetString("chat-manager-send-cult-chat-wrap-message",
            ("channelName", Loc.GetString("chat-manager-cult-channel-name")),
            ("player", playerName),
            ("message", FormattedMessage.EscapeText(message)));

        _chatManager.ChatMessageToMany(ChatChannel.Telepathic,
            message,
            wrappedMessage,
            source,
            hideChat,
            true,
            clients.ToList(),
            language.SpeechOverride.Color);
    }

    private IEnumerable<INetChannel> GetClients(string languageId)
    {
        return Filter.Empty()
            .AddWhereAttachedEntity(entity => CanHearBloodCult(entity, languageId))
            .Recipients
            .Union(_adminManager.ActiveAdmins)
            .Select(p => p.Channel);
    }

    private bool CanHearBloodCult(EntityUid entity, string languageId)
    {
        // Dumont
        return _language.CanUnderstand(entity, languageId);
    }
}
