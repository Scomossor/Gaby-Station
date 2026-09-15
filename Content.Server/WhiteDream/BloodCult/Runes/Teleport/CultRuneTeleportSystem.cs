// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.WhiteDream.BloodCult.UI;
using Content.Server.Popups;
using Content.Shared.UserInterface;
using Content.Shared.WhiteDream.BloodCult.UI;
using Content.Shared._White.ListViewSelector;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.WhiteDream.BloodCult.Runes.Teleport;

public sealed partial class CultRuneTeleportSystem : EntitySystem
{
    // WhiteDream - teleport visuals
    private static readonly EntProtoId TeleportInEffect = "CultTeleportInEffect";
    private static readonly EntProtoId TeleportOutEffect = "CultTeleportOutEffect";

    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private CultRuneBaseSystem _cultRune = default!;
    [Dependency] private DeferredUiOpenSystem _deferredUi = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultRuneTeleportComponent, AfterRunePlaced>(OnAfterRunePlaced);
        SubscribeLocalEvent<CultRuneTeleportComponent, NameSelectedMessage>(OnNameSelected);
        SubscribeLocalEvent<CultRuneTeleportComponent, BoundUIClosedEvent>(OnNameSelectorClosed);
        SubscribeLocalEvent<CultRuneTeleportComponent, TryInvokeCultRuneEvent>(OnTeleportRuneInvoked);
        SubscribeLocalEvent<CultRuneTeleportComponent, ListViewItemSelectedMessage>(OnTeleportRuneSelected);
        SubscribeLocalEvent<CultRuneTeleportComponent, BoundUIOpenedEvent>(OnTeleportUiOpened); // Dumont
    }

    private void OnAfterRunePlaced(Entity<CultRuneTeleportComponent> rune, ref AfterRunePlaced args)
    {
        _ui.OpenUi(rune.Owner, NameSelectorUiKey.Key, args.User);
    }

    private void OnNameSelected(Entity<CultRuneTeleportComponent> rune, ref NameSelectedMessage args)
    {
        rune.Comp.Name = ResolveTeleportRuneName(args.Name);
    }

    private void OnNameSelectorClosed(Entity<CultRuneTeleportComponent> rune, ref BoundUIClosedEvent args)
    {
        if (args.UiKey is not NameSelectorUiKey || !string.IsNullOrWhiteSpace(rune.Comp.Name))
            return;

        rune.Comp.Name = ResolveTeleportRuneName(null);
    }

    private void OnTeleportRuneInvoked(Entity<CultRuneTeleportComponent> rune, ref TryInvokeCultRuneEvent args)
    {
        var runeUid = rune.Owner;
        if (_ui.IsUiOpen(runeUid, ListViewSelectorUiKey.Key))
        {
            args.Cancel();
            return;
        }

        if (!TryGetTeleportRunes(args.User, out var runes, runeUid))
        {
            args.Cancel();
            return;
        }

        // Dumont
        EnsureComp<CultListSelectorComponent>(runeUid).Entries = runes;
        Dirty(runeUid, Comp<CultListSelectorComponent>(runeUid));
        _deferredUi.OpenNextTick(runeUid, ListViewSelectorUiKey.Key, args.User);
    }

    private void OnTeleportUiOpened(Entity<CultRuneTeleportComponent> rune, ref BoundUIOpenedEvent args)
    {
        // Dumont
        if (args.UiKey is not ListViewSelectorUiKey)
            return;

        if (TryGetTeleportRunes(args.Actor, out var runes, rune.Owner))
            _ui.SetUiState(rune.Owner, ListViewSelectorUiKey.Key, new ListViewSelectorState(runes));
    }

    private void OnTeleportRuneSelected(Entity<CultRuneTeleportComponent> origin, ref ListViewItemSelectedMessage args)
    {
        if (!EntityUid.TryParse(args.SelectedItem.Id, out var destination))
            return;

        var teleportTargets = _cultRune.GetTargetsNearRune(origin, origin.Comp.TeleportGatherRange);
        var destinationTransform = Transform(destination);

        foreach (var target in teleportTargets)
        {
            _cultRune.StopPulling(target);
            // WhiteDream - visual effect on both ends
            Spawn(TeleportOutEffect, Transform(target).Coordinates);
            _transform.SetCoordinates(target, destinationTransform.Coordinates);
            Spawn(TeleportInEffect, Transform(target).Coordinates);
        }

        _audio.PlayPvs(origin.Comp.TeleportOutSound, origin);
        _audio.PlayPvs(origin.Comp.TeleportInSound, destination);
    }

    public bool TryGetTeleportRunes(EntityUid user, out List<ListViewSelectorEntry> runes, EntityUid? runeUid = null)
    {
        var runeQuery = EntityQueryEnumerator<CultRuneTeleportComponent>();
        runes = new List<ListViewSelectorEntry>();
        while (runeQuery.MoveNext(out var targetRune, out var teleportRune))
        {
            if (targetRune == runeUid)
                continue;

            var entry = new ListViewSelectorEntry(targetRune.ToString(), ResolveTeleportRuneName(teleportRune.Name));
            runes.Add(entry);
        }

        if (runes.Count != 0)
            return true;

        _popup.PopupEntity(Loc.GetString("cult-teleport-not-found"), user, user);
        return false;
    }

    private string ResolveTeleportRuneName(string? name) =>
        string.IsNullOrWhiteSpace(name) ? Loc.GetString("cult-teleport-rune-unnamed") : name.Trim();
}
