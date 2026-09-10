// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Damage.Systems;

namespace Content.Trauma.Shared.Genetics.Abilities;

public sealed class ArmorMutationSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArmorMutationComponent, DamageModifyEvent>(OnDamageModify);
    }

    private void OnDamageModify(EntityUid uid, ArmorMutationComponent comp, DamageModifyEvent args)
    {
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, comp.Modifiers);
    }
}
