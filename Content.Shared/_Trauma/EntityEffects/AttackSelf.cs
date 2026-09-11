// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.CombatMode;
using Content.Shared.EntityEffects;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Makes the target entity melee attack itself.
/// </summary>
public sealed partial class AttackSelf : EventEntityEffect<AttackSelf>
{
    /// <summary>
    /// Try to use the held item instead of a punch attack.
    /// </summary>
    [DataField]
    public bool UseHeld = true;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class AttackSelfEffectSystem : TraumaEntityEffectSystem<CombatModeComponent, AttackSelf>
{
    [Dependency] private SharedCombatModeSystem _combatMode = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedMeleeWeaponSystem _melee = default!;

    protected override void Effect(Entity<CombatModeComponent> ent, AttackSelf effect, EntityEffectBaseArgs args)
    {
        var user = ent.Owner;
        var weapon = effect.UseHeld ? _hands.GetActiveItemOrSelf(user) : user;
        if (!TryComp<MeleeWeaponComponent>(weapon, out var weaponComp))
            return;

        var wasOn = ent.Comp.IsInCombatMode;
        _combatMode.SetInCombatMode(ent, true, ent.Comp); // need to turn on combat mode or it won't attack
        _melee.AttemptLightAttack(user, weapon, weaponComp, user); // stop hitting yourself!
        _combatMode.SetInCombatMode(ent, wasOn, ent.Comp); // restore it to last setting
    }
}

/// <summary>
/// Makes the target entity attack a random mob nearby.
/// </summary>
/// <remarks>
/// </remarks>
public sealed partial class AttackOthers : EventEntityEffect<AttackOthers>
{
    /// <summary>
    /// Try to use the held item instead of a punch attack.
    /// </summary>
    [DataField]
    public bool UseHeld = true;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}
