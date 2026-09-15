// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;

namespace Content.Trauma.Shared.Damage;

public sealed class FragileSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FragileComponent, DamageModifyEvent>(OnDamageModify);
    }

    private void OnDamageModify(Entity<FragileComponent> ent, ref DamageModifyEvent args)
    {
        if (args.Target != ent.Owner || !args.Damage.AnyPositive())
            return;

        args.Damage *= ent.Comp.Modifier; // die
    }
}
