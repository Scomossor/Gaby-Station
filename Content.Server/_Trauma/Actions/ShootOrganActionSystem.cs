// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Body.Systems;
using Content.Server.Polymorph.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Trauma.Shared.Actions;

namespace Content.Trauma.Server.Actions;

public sealed partial class ShootOrganActionSystem : SharedShootOrganActionSystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShootOrganActionComponent, ShootOrganActionEvent>(OnShootOrganAction);
    }

    private void OnShootOrganAction(Entity<ShootOrganActionComponent> ent, ref ShootOrganActionEvent args)
    {
        args.Handled = true;

        var user = args.Performer;
        if (RemoveOrgan(ent, user) is not { } organ)
        {
            _popup.PopupEntity(Loc.GetString("MutationTongueSpike-popup-no-organ", ("organ", ent.Comp.Organ)), user, user);
            return;
        }

        if (_polymorph.PolymorphEntity(organ, ent.Comp.Polymorph) is not { } projectile)
            return;

        var projComp = EnsureComp<ActionProjectileComponent>(projectile);
        projComp.Container = args.Action.Comp.Container;
        Dirty(projectile, projComp);

        _throwing.TryThrow(projectile, args.Target, user: user);
    }

    private EntityUid? RemoveOrgan(Entity<ShootOrganActionComponent> ent, EntityUid user)
    {
        if (!TryComp<BodyComponent>(user, out var body))
            return null;

        foreach (var (organId, organ) in _body.GetBodyOrgans(user, body))
        {
            if (!_tag.HasTag(organId, ent.Comp.Organ))
                continue;

            if (_body.RemoveOrgan(organId, organ))
                return organId;
        }

        return null;
    }
}
