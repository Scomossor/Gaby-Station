// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Mind;
using Content.Server._EinsteinEngines.Language;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Roles.Jobs;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Content.Shared.WhiteDream.BloodCult.Commune;
using Robust.Server.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.WhiteDream.BloodCult.Commune;

/// <summary>
///     Lets cultists whisper an incantation and have the message reach every other cultist,
///     no matter the distance.
/// </summary>
public sealed partial class BloodCultCommuneSystem : EntitySystem
{
    /// <summary>
    ///     Number of cult-chant-X entries in commune.ftl.
    /// </summary>
    private const int TotalChants = 16;

    private static readonly Color CommuneColor = new(166, 27, 27, 255);

    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private IRobustRandom _random = default!;

    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private LanguageSystem _language = default!;
    [Dependency] private SharedJobSystem _jobs = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultistComponent, ComponentStartup>(OnCultistStartup);
        SubscribeLocalEvent<BloodCultistComponent, BloodCultCommuneEvent>(OnCommuneAction);
        SubscribeLocalEvent<BloodCultistComponent, BloodCultCommuneSendMessage>(OnCommuneSend);
    }

    private void OnCultistStartup(Entity<BloodCultistComponent> cultist, ref ComponentStartup args)
    {
        // The commune window lives on the cultist themselves.
        _ui.SetUi(cultist.Owner,
            BloodCultCommuneUiKey.Key,
            new InterfaceData("BloodCultCommuneBoundUserInterface"));
    }

    private void OnCommuneAction(Entity<BloodCultistComponent> cultist, ref BloodCultCommuneEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        _ui.TryToggleUi(cultist.Owner, BloodCultCommuneUiKey.Key, cultist.Owner);
    }

    private void OnCommuneSend(Entity<BloodCultistComponent> cultist, ref BloodCultCommuneSendMessage args)
    {
        var message = args.Message.Trim();
        if (string.IsNullOrWhiteSpace(message))
            return;

        _ui.CloseUi(cultist.Owner, BloodCultCommuneUiKey.Key, cultist.Owner);
        DistributeCommune(cultist.Owner, message);
    }

    /// <summary>
    ///     Whispers a short incantation out loud (so the uninitiated can catch you doing it),
    ///     then delivers the actual message to every cultist.
    /// </summary>
    public void DistributeCommune(EntityUid sender, string message)
    {
        if (!_mind.TryGetMind(sender, out var mindId, out _))
            return;

        // Speaking the incantation out loud is the cost of using the commune.
        _chat.TrySendInGameICMessage(sender,
            GenerateChant(1),
            InGameICChatType.Whisper,
            ChatTransmitRange.Normal);

        var job = _jobs.MindTryGetJobName(mindId);
        var announcement = Loc.GetString("cult-commune-message",
            ("name", Name(sender)),
            ("job", job),
            ("message", FormattedMessage.EscapeText(message)));

        AnnounceToCultists(sender, announcement);

        _adminLogger.Add(LogType.Chat,
            LogImpact.Low,
            $"{ToPrettyString(sender):player} sent a blood cult commune: {message}");
    }

    /// <summary>
    ///     Sends a raw, already localized line to everyone who understands the cult language.
    ///     Also usable by the gamerule for progression announcements.
    /// </summary>
    public void AnnounceToCultists(EntityUid source, string message, int fontSize = 12)
    {
        var languageId = TryComp<BloodCultistComponent>(source, out var cultist)
            ? cultist.CultLanguageId.Id
            : "Eldritch";
        var wrappedMessage = $"[font size={fontSize}][bold]{message}[/bold][/font]";

        _chatManager.ChatMessageToMany(ChatChannel.CollectiveMind,
            message,
            wrappedMessage,
            source,
            false,
            true,
            GetCultClients(languageId).ToList(),
            CommuneColor);
    }

    /// <summary>
    ///     Builds a random incantation out of the cult chant fragments.
    /// </summary>
    public string GenerateChant(int wordCount = 2)
    {
        if (wordCount < 1)
            wordCount = 1;

        var parts = new string[wordCount];
        for (var i = 0; i < wordCount; i++)
            parts[i] = Loc.GetString($"cult-chant-{_random.Next(1, TotalChants + 1)}");

        return string.Join(" ", parts);
    }

    /// <summary>
    ///     Sends an already localized line to a single cultist. Used for private status reports.
    /// </summary>
    public void AnnounceToCultist(EntityUid target, string message, int fontSize = 12, Color? color = null)
    {
        if (!TryComp<ActorComponent>(target, out var actor))
            return;

        var wrappedMessage = $"[font size={fontSize}]{message}[/font]";

        _chatManager.ChatMessageToOne(ChatChannel.CollectiveMind,
            message,
            wrappedMessage,
            target,
            false,
            actor.PlayerSession.Channel,
            color ?? CommuneColor);
    }

    private IEnumerable<INetChannel> GetCultClients(string languageId)
    {
        return Filter.Empty()
            .AddWhereAttachedEntity(entity => HasComp<BloodCultistComponent>(entity)
                                              || _language.CanUnderstand(entity, languageId))
            .Recipients
            .Union(_adminManager.ActiveAdmins)
            .Select(p => p.Channel);
    }
}
