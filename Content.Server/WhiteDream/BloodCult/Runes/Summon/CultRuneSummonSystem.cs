// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.WhiteDream.BloodCult.UI;
using Content.Server.WhiteDream.BloodCult.UI;
using System.Linq;
using Content.Server.Popups;
using Content.Shared.Cuffs.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Content.Shared._White.ListViewSelector;
using Robust.Server.Audio;
using Robust.Server.GameObjects;

namespace Content.Server.WhiteDream.BloodCult.Runes.Summon;

public sealed partial class CultRuneSummonSystem : EntitySystem
{
    [Dependency] private DeferredUiOpenSystem _deferredUi = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private CultRuneBaseSystem _cultRune = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultRuneSummonComponent, TryInvokeCultRuneEvent>(OnSummonRuneInvoked);
        SubscribeLocalEvent<CultRuneSummonComponent, ListViewItemSelectedMessage>(OnCultistSelected);
    }

    private void OnSummonRuneInvoked(Entity<CultRuneSummonComponent> rune, ref TryInvokeCultRuneEvent args)
    {
        var runeUid = rune.Owner;
        if (_ui.IsUiOpen(runeUid, ListViewSelectorUiKey.Key))
        {
            args.Cancel();
            return;
        }

        var cultistsQuery = EntityQueryEnumerator<BloodCultistComponent>();
        var cultist = new List<ListViewSelectorEntry>();
        var invokers = args.Invokers.ToArray();
        while (cultistsQuery.MoveNext(out var cultistUid, out _))
        {
            if (invokers.Contains(cultistUid))
                continue;

            var metaData = MetaData(cultistUid);
            var entry = new ListViewSelectorEntry(cultistUid.ToString(),
                metaData.EntityName,
                metaData.EntityDescription);

            cultist.Add(entry);
        }

        if (cultist.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("cult-rune-no-targets"), args.User, args.User);
            args.Cancel();
            return;
        }

        // Dumont
        EnsureComp<CultListSelectorComponent>(runeUid).Entries = cultist;
        Dirty(runeUid, Comp<CultListSelectorComponent>(runeUid));
        _deferredUi.OpenNextTick(runeUid, ListViewSelectorUiKey.Key, args.User);
    }

    private void OnCultistSelected(Entity<CultRuneSummonComponent> ent, ref ListViewItemSelectedMessage args)
    {
        if (!EntityUid.TryParse(args.SelectedItem.Id, out var target))
            return;

        if (TryComp(target, out PullableComponent? pullable) && pullable.BeingPulled)
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-summon-being-pulled"), ent, args.Actor);
            return;
        }

        if (TryComp(target, out CuffableComponent? cuffable) && cuffable.CuffedHandCount > 0)
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-summon-cuffed"), ent, args.Actor);
            return;
        }

        var runeTransform = Transform(ent);

        _cultRune.StopPulling(target);

        _transform.SetCoordinates(target, runeTransform.Coordinates);

        _audio.PlayPvs(ent.Comp.TeleportSound, ent);
    }
}
