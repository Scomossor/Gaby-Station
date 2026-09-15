// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.WhiteDream.BloodCult.UI;
using Content.Server.WhiteDream.BloodCult.UI;
using Content.Server.DoAfter;
using Content.Server.WhiteDream.BloodCult.Runes;
using Content.Server.WhiteDream.BloodCult.Runes.Teleport;
using Content.Shared.DoAfter;
using Content.Shared.WhiteDream.BloodCult.Spells;
using Content.Shared._White.ListViewSelector;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.WhiteDream.BloodCult.Spells;

public sealed partial class BloodCultTeleportSpellSystem : EntitySystem
{
    // WhiteDream - teleport visuals
    private static readonly EntProtoId TeleportInEffect = "CultTeleportInEffect";
    private static readonly EntProtoId TeleportOutEffect = "CultTeleportOutEffect";

    [Dependency] private DeferredUiOpenSystem _deferredUi = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private CultRuneBaseSystem _cultRune = default!;
    [Dependency] private CultRuneTeleportSystem _runeTeleport = default!;
    [Dependency] private DoAfterSystem _doAfter = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BloodCultTeleportEvent>(OnTeleport);
        SubscribeLocalEvent<BloodCultSpellsHolderComponent, ListViewItemSelectedMessage>(OnTeleportRuneSelected);
        SubscribeLocalEvent<BloodCultSpellsHolderComponent, TeleportActionDoAfterEvent>(OnTeleportDoAfter);
    }

    private void OnTeleport(BloodCultTeleportEvent ev)
    {
        if (ev.Handled || !_runeTeleport.TryGetTeleportRunes(ev.Performer, out var runes))
            return;

        // Dumont changes start
        if (!TryComp<BloodCultSpellsHolderComponent>(ev.Performer, out var holder))
            return;

        holder.TeleportTarget = ev.Target;
        holder.TeleportDuration = ev.DoAfterDuration;

        EnsureComp<CultListSelectorComponent>(ev.Performer).Entries = runes;
        Dirty(ev.Performer, Comp<CultListSelectorComponent>(ev.Performer));
        _deferredUi.OpenNextTick(ev.Performer, ListViewSelectorUiKey.Key, ev.Performer);
        // Dumont end

        ev.Handled = true;
    }

    private void OnTeleportRuneSelected(
        Entity<BloodCultSpellsHolderComponent> ent,
        ref ListViewItemSelectedMessage args
    )
    {
        // Dumont changes start
        if (ent.Comp.TeleportTarget is not { } target || TerminatingOrDeleted(target))
            return;

        if (!EntityUid.TryParse(args.SelectedItem.Id, out var rune))
            return;

        ent.Comp.TeleportTarget = null;

        var teleportDoAfter = new TeleportActionDoAfterEvent { Rune = GetNetEntity(rune) };
        var doAfterArgs = new DoAfterArgs(EntityManager,
            ent.Owner,
            ent.Comp.TeleportDuration,
            teleportDoAfter,
            target,
            target);
        // Dumont end

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnTeleportDoAfter(Entity<BloodCultSpellsHolderComponent> user, ref TeleportActionDoAfterEvent ev)
    {
        if (ev.Target is not { } target)
            return;

        var rune = GetEntity(ev.Rune);
        _audio.PlayPvs(ev.TeleportOutSound, target);

        _cultRune.StopPulling(target);
        // WhiteDream - visual effect on both ends
        Spawn(TeleportOutEffect, Transform(target).Coordinates);
        _transform.SetCoordinates(target, Transform(rune).Coordinates);
        Spawn(TeleportInEffect, Transform(target).Coordinates);

        _audio.PlayPvs(ev.TeleportInSound, rune);
    }
}
