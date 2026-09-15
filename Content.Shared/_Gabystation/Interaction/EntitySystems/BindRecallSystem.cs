// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// Marking the file for future changes

using Content.Shared._Gabystation.Interaction.Events;
using Content.Shared._Gabystation.Interaction.Components;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Verbs;

namespace Content.Shared._Gabystation.Interaction.EntitySystems;

public sealed class BindRecallSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RecallableComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerb);
        SubscribeLocalEvent<RecallableComponent, BindRecallDoAfterEvent>(OnBindDoAfter);
        SubscribeLocalEvent<RecallableComponent, UnbindRecallDoAfterEvent>(OnUnbindDoAfter);
        SubscribeLocalEvent<RecallableComponent, ExaminedEvent>(OnExamine);
    }

    private void OnGetVerb(Entity<RecallableComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanInteract)
            return;

        var user = args.User;

        // ------------------
        // Bind verb
        // ------------------
        if (ent.Comp.LinkedEntity is null)
        {
            var bindVerb = new ActivationVerb
            {
                Text = Loc.GetString("recall-item-bind"),
                Act = () =>
                {
                    var recall = EnsureComp<RecallLinkComponent>(user);

                    // Check BEFORE the DoAfter
                    if (recall.BoundItem is not null)
                    {
                        _popup.PopupEntity(Loc.GetString("recall-item-already-have"), user, user);
                        return;
                    }

                    var doAfter = new DoAfterArgs(EntityManager, user, 5f,
                        new BindRecallDoAfterEvent(),
                        ent.Owner)
                    {
                        BreakOnMove = true,
                        BreakOnDamage = true,
                        NeedHand = true
                    };

                    _doAfter.TryStartDoAfter(doAfter);
                }
            };

            args.Verbs.Add(bindVerb);
        }

        if (ent.Comp.LinkedEntity != user
            || !TryComp<RecallLinkComponent>(user, out var recallComp))
            return;

        // ------------------
        // Unbind verb
        // ------------------
        var unbindVerb = new ActivationVerb
        {
            Text = Loc.GetString("recall-item-unbind"),
            Act = () =>
            {
                var doAfter = new DoAfterArgs(EntityManager, user, 5f,
                    new UnbindRecallDoAfterEvent(),
                    ent.Owner)
                {
                    BreakOnMove = true,
                    BreakOnDamage = true,
                    NeedHand = true
                };

                _doAfter.TryStartDoAfter(doAfter);
            }
        };

        args.Verbs.Add(unbindVerb);
    }

    private void OnBindDoAfter(Entity<RecallableComponent> ent, ref BindRecallDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        var user = args.User;

        var recall = EnsureComp<RecallLinkComponent>(user);

        if (recall.BoundItem is not null)
            return;

        // Verify if another user has already bound
        if (ent.Comp.LinkedEntity is not null)
        {
            _popup.PopupEntity(Loc.GetString("recall-item-already-bound"), user, user);
            return;
        }

        EntityUid? maybeAction = null;
        _actions.AddAction(user, ref maybeAction, recall.RecallAction);

        if (maybeAction is not { } action)
            return;

        recall.BoundItem = ent.Owner;
        recall.RecallActionEntity = action;

        ent.Comp.LinkedEntity = user;
        Dirty(ent);

        var itemName = Name(ent.Owner);

        _metaData.SetEntityName(action,
            Loc.GetString("recall-item-action-name", ("item", itemName)));

        _metaData.SetEntityDescription(action,
            Loc.GetString("recall-item-action-desc", ("item", itemName)));
    }

    private void OnUnbindDoAfter(Entity<RecallableComponent> ent, ref UnbindRecallDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        var user = args.User;

        if (!TryComp<RecallLinkComponent>(user, out var recallComp))
            return;

        // Verify if the unbinding item is the one bound to the user
        if (recallComp.BoundItem != ent.Owner)
            return;

        if (recallComp.RecallActionEntity is { } actionEnt && Exists(actionEnt))
            QueueDel(actionEnt);

        recallComp.BoundItem = null;
        recallComp.RecallActionEntity = null;
        Dirty(user, recallComp);

        ent.Comp.LinkedEntity = null;
        Dirty(ent);
    }

    private void OnExamine(Entity<RecallableComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Examinable)
            return;

        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.LinkedEntity is null)
            args.PushMarkup(Loc.GetString("recall-bound-item-examine-free"));
        else
            args.PushMarkup(Loc.GetString("recall-bound-item-examine-owned"));
    }
}
