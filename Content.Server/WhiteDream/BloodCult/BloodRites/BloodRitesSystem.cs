// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Maths.FixedPoint;
using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Fluids.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Stunnable;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Content.Shared.WhiteDream.BloodCult.BloodRites;
using Content.Shared.WhiteDream.BloodCult.Spells;
using Content.Shared.WhiteDream.BloodCult.UI;
using Content.Shared._Shitmed.Targeting;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.WhiteDream.BloodCult.BloodRites;

public sealed partial class BloodRitesSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _protoManager = default!;

    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private HandsSystem _handsSystem = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private DoAfterSystem _doAfter = default!; // WhiteDream - drain do-after

    private readonly ProtoId<ReagentPrototype> _bloodProto = "Blood";

    public override void Initialize()
    {
        SubscribeLocalEvent<BloodRitesAuraComponent, ExaminedEvent>(OnExamining);

        SubscribeLocalEvent<BloodRitesAuraComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<BloodRitesAuraComponent, MeleeHitEvent>(OnMeleeHit);

        SubscribeLocalEvent<BloodRitesAuraComponent, BeforeActivatableUIOpenEvent>(BeforeUiOpen);
        SubscribeLocalEvent<BloodRitesAuraComponent, BloodRitesMessage>(OnRitesMessage);

        SubscribeLocalEvent<BloodRitesAuraComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<BloodRitesAuraComponent, BloodRitesDrainDoAfterEvent>(OnDrainDoAfter);
    }

    private void OnExamining(Entity<BloodRitesAuraComponent> rites, ref ExaminedEvent args) =>
        args.PushMarkup(Loc.GetString("blood-rites-stored-blood", ("amount", rites.Comp.StoredBlood.ToString())));

    private void OnAfterInteract(Entity<BloodRitesAuraComponent> rites, ref AfterInteractEvent args)
    {
        if (!args.Target.HasValue || args.Handled)
            return;

        if (HasComp<BloodCultistComponent>(args.Target))
        {
            if (TryHealCultist(rites, args.User, args.Target.Value))
            {
                _audio.PlayPvs(rites.Comp.BloodRitesAudio, rites);
                args.Handled = true;
            }

            return;
        }

        if (HasComp<BloodstreamComponent>(args.Target))
        {
            // <WhiteDream> - draining a living victim needs them restrained, and takes a while
            var victim = args.Target.Value;
            if (!IsRestrained(victim))
            {
                _popup.PopupEntity(Loc.GetString("blood-rites-drain-not-restrained"), victim, args.User);
                args.Handled = true;
                return;
            }

            var doAfterArgs = new DoAfterArgs(EntityManager,
                args.User,
                rites.Comp.DrainDuration,
                new BloodRitesDrainDoAfterEvent(),
                rites.Owner,
                victim,
                rites.Owner)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true,
                MovementThreshold = 0.5f,
            };

            if (_doAfter.TryStartDoAfter(doAfterArgs))
                _popup.PopupEntity(Loc.GetString("blood-rites-drain-started"), victim, args.User);

            args.Handled = true;
            return;
            // </WhiteDream>
        }

        if (HasComp<PuddleComponent>(args.Target))
        {
            ConsumePuddles(args.Target.Value, rites);
            args.Handled = true;
        }
        else if (TryComp(args.Target, out SolutionContainerManagerComponent? solutionContainer))
        {
            ConsumeBloodFromSolution((args.Target.Value, solutionContainer), rites);
            args.Handled = true;
        }
    }

    private void OnMeleeHit(Entity<BloodRitesAuraComponent> rites, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        if (args.HitEntities.Count == 0)
        {
            TryConsumePuddlesAtCoordinates(args.Coords, rites);
            return;
        }

        var playSound = false;

        foreach (var target in args.HitEntities)
        {
            if (target == args.User)
                continue;

            if (HasComp<BloodCultistComponent>(target))
            {
                if (TryHealCultist(rites, args.User, target))
                    playSound = true;

                continue;
            }

            if (HasComp<PuddleComponent>(target))
            {
                ConsumePuddles(target, rites);
                playSound = true;
                continue;
            }

            // WhiteDream - same restraint rule as the interact path, otherwise you could just melee the blood out
            if (HasComp<BloodstreamComponent>(target) && IsRestrained(target) && TryExtractBlood(rites, target))
            {
                playSound = true;
                args.Handled = true;
            }
        }

        if (playSound)
            _audio.PlayPvs(rites.Comp.BloodRitesAudio, rites);
    }

    private bool TryExtractBlood(Entity<BloodRitesAuraComponent> rites, EntityUid target)
    {
        if (!TryComp(target, out BloodstreamComponent? bloodstream) ||
            bloodstream.BloodSolution is not { } solution)
            return false;

        // Dumont
        var extracted = solution.Comp.Solution.RemoveReagent(
            bloodstream.BloodReagent,
            rites.Comp.BloodExtractionAmount,
            ignoreReagentData: true);

        if (extracted <= FixedPoint2.Zero)
            return false;

        _solutionContainer.UpdateChemicals(solution);
        rites.Comp.StoredBlood += extracted;
        Dirty(target, bloodstream);
        return true;
    }

    private bool TryHealCultist(Entity<BloodRitesAuraComponent> rites, EntityUid user, EntityUid target)
    {
        var healed = false;

        if (TryComp(target, out BloodstreamComponent? bloodstream) &&
            RestoreBloodLevel(rites, user, (target, bloodstream)))
        {
            healed = true;
        }

        if (TryComp(target, out DamageableComponent? damageable) && Heal(rites, user, (target, damageable)))
            healed = true;

        return healed;
    }

    private void TryConsumePuddlesAtCoordinates(EntityCoordinates coordinates, Entity<BloodRitesAuraComponent> rites)
    {
        var lookup = _lookup.GetEntitiesInRange<PuddleComponent>(
            coordinates,
            rites.Comp.PuddleConsumeRadius,
            LookupFlags.Uncontained);

        if (lookup.Count == 0)
            return;

        foreach (var puddle in lookup)
            ConsumePuddles(puddle, rites);

        _audio.PlayPvs(rites.Comp.BloodRitesAudio, rites);
    }

    private void BeforeUiOpen(Entity<BloodRitesAuraComponent> rites, ref BeforeActivatableUIOpenEvent args)
    {
        var state = new BloodRitesUiState(rites.Comp.Crafts, rites.Comp.StoredBlood);
        _ui.SetUiState(rites.Owner, BloodRitesUiKey.Key, state);
    }

    private void OnRitesMessage(Entity<BloodRitesAuraComponent> rites, ref BloodRitesMessage args)
    {
        // The selected prototype comes from the client. Validate both the choice and its cost
        // before consuming the aura so a forged UI message cannot create arbitrary entities.
        if (!HasComp<BloodCultistComponent>(args.Actor) ||
            !rites.Comp.Crafts.TryGetValue(args.SelectedProto, out var cost) ||
            FixedPoint2.New(cost) > rites.Comp.StoredBlood)
        {
            _popup.PopupEntity(Loc.GetString("blood-rites-not-enough-blood"), rites, args.Actor);
            return;
        }

        Del(rites);

        var ent = Spawn(args.SelectedProto, _transform.GetMapCoordinates(args.Actor));
        _handsSystem.TryPickup(args.Actor, ent);
    }

    // <WhiteDream> - drain do-after
    private void OnDrainDoAfter(Entity<BloodRitesAuraComponent> rites, ref BloodRitesDrainDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } victim)
            return;

        args.Handled = true;

        if (!IsRestrained(victim))
        {
            _popup.PopupEntity(Loc.GetString("blood-rites-drain-not-restrained"), victim, args.User);
            return;
        }

        if (!TryExtractBlood(rites, victim))
            return;

        _audio.PlayPvs(rites.Comp.BloodRitesAudio, rites);
    }

    /// <summary>
    ///     A victim can only be drained while cuffed, stunned or knocked down.
    /// </summary>
    private bool IsRestrained(EntityUid target)
    {
        if (TryComp<CuffableComponent>(target, out var cuffable) && cuffable.CuffedHandCount > 0)
            return true;

        return HasComp<StunnedComponent>(target) || HasComp<KnockedDownComponent>(target);
    }
    // </WhiteDream>

    private void OnDropped(Entity<BloodRitesAuraComponent> rites, ref DroppedEvent args) => QueueDel(rites);

    private bool Heal(Entity<BloodRitesAuraComponent> rites, EntityUid user, Entity<DamageableComponent> target)
    {
        if (target.Comp.TotalDamage == 0) // Dumont
            return false;

        if (TryComp(target, out MobStateComponent? mobState) && mobState.CurrentState == MobState.Dead)
        {
            _popup.PopupEntity(Loc.GetString("blood-rites-heal-dead"), target, user);
            return false;
        }

        if (!HasComp<BloodstreamComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("blood-rites-heal-no-bloodstream"), target, user);
            return false;
        }

        var bloodCost = rites.Comp.HealingCost;
        if (target.Owner == user)
            bloodCost *= rites.Comp.SelfHealRatio;

        if (bloodCost > rites.Comp.StoredBlood)
        {
            _popup.PopupEntity(Loc.GetString("blood-rites-not-enough-blood"), rites, user);
            return false;
        }

        var healingLeft = rites.Comp.TotalHealing;

        foreach (var (type, value) in target.Comp.Damage.DamageDict) // Dumont
        {
            // somehow?
            if (!_protoManager.TryIndex(type, out DamageTypePrototype? damageType))
                continue;

            var toHeal = value;
            if (toHeal > healingLeft)
                toHeal = healingLeft;

            _damageable.TryChangeDamage(target.Owner,
                new DamageSpecifier(damageType, -toHeal),
                true,
                targetPart: TargetBodyPart.All); // Dumont

            healingLeft -= toHeal;
            if (healingLeft == 0)
                break;
        }

        rites.Comp.StoredBlood -= bloodCost;
        return true;
    }

    private bool RestoreBloodLevel(
        Entity<BloodRitesAuraComponent> rites,
        EntityUid user,
        Entity<BloodstreamComponent> target
    )
    {
        if (target.Comp.BloodSolution is null)
            return false;

        var missingBlood = target.Comp.BloodSolution.Value.Comp.Solution.AvailableVolume;
        if (missingBlood == 0)
            return false;

        var bloodCost = missingBlood * rites.Comp.BloodRegenerationRatio;
        if (target.Owner == user)
            bloodCost *= rites.Comp.SelfHealRatio;

        if (bloodCost > rites.Comp.StoredBlood)
        {
            _popup.PopupEntity(Loc.GetString("blood-rites-no-blood-left"), rites, user);
            bloodCost = rites.Comp.StoredBlood;
        }

        if (bloodCost <= FixedPoint2.Zero)
            return false;

        _bloodstream.TryModifyBleedAmount(target.AsNullable(), -3);
        _bloodstream.TryModifyBloodLevel(target.AsNullable(), bloodCost / rites.Comp.BloodRegenerationRatio);

        rites.Comp.StoredBlood -= bloodCost;
        return true;
    }

    private void ConsumePuddles(EntityUid origin, Entity<BloodRitesAuraComponent> rites)
    {
        var coords = Transform(origin).Coordinates;

        var lookup = _lookup.GetEntitiesInRange<PuddleComponent>(
            coords,
            rites.Comp.PuddleConsumeRadius,
            LookupFlags.Uncontained);

        foreach (var puddle in lookup)
        {
            if (!TryComp(puddle, out SolutionContainerManagerComponent? solutionContainer))
                continue;
            ConsumeBloodFromSolution((puddle, solutionContainer), rites);
        }

        _audio.PlayPvs(rites.Comp.BloodRitesAudio, rites);
    }

    private void ConsumeBloodFromSolution(
        Entity<SolutionContainerManagerComponent?> ent,
        Entity<BloodRitesAuraComponent> rites
    )
    {
        foreach (var (_, solution) in _solutionContainer.EnumerateSolutions(ent))
        {
            rites.Comp.StoredBlood += solution.Comp.Solution.RemoveReagent(
                _bloodProto,
                1000,
                ignoreReagentData: true);
            _solutionContainer.UpdateChemicals(solution);
            break;
        }
    }
}
