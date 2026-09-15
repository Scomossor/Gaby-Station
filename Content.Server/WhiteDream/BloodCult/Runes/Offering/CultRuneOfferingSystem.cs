// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Common.Religion;
using Content.Server.Body.Systems;
using Content.Server.Cuffs;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.WhiteDream.BloodCult.Gamerule;
using Content.Server.WhiteDream.BloodCult.Runes.Revive;
using Content.Server._EinsteinEngines.Language;
using Content.Shared.Bible.Components;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Gibbing;
using Content.Shared.Mindshield;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.WhiteDream.BloodCult.Runes.Offering;

public sealed partial class CultRuneOfferingSystem : EntitySystem
{
    private const string MutedKey = "Muted"; // Dumont

    [Dependency] private BloodCultRuleSystem _bloodCultRule = default!;
    [Dependency] private LanguageSystem _language = default!;
    [Dependency] private BodySystem _body = default!; // Dumont
    [Dependency] private CuffableSystem _cuffable = default!;
    [Dependency] private CultRuneBaseSystem _cultRune = default!;
    [Dependency] private CultRuneReviveSystem _cultRuneRevive = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!; // Dumont
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CultRuneOfferingComponent, TryInvokeCultRuneEvent>(OnOfferingRuneInvoked);
    }

    private void OnOfferingRuneInvoked(Entity<CultRuneOfferingComponent> rune, ref TryInvokeCultRuneEvent args)
    {
        var possibleTargets = _cultRune.GetTargetsNearRune(
            rune,
            rune.Comp.OfferingRange,
            entity => HasComp<BloodCultistComponent>(entity));

        if (possibleTargets.Count == 0)
        {
            args.Cancel();
            return;
        }

        var target = possibleTargets.First();
        if (!TryOffer(rune, target, args.User, args.Invokers.Count))
            args.Cancel();
    }

    private bool TryOffer(Entity<CultRuneOfferingComponent> rune, EntityUid target, EntityUid user, int invokersTotal)
    {
        // WhiteDream - Nar'Sie's marked offering always costs three of us, alive or dead. Any other
        // corpse only needs one.
        if (_bloodCultRule.IsTarget(target))
            return TrySacrifice(rune, target, invokersTotal);

        if (_mobState.IsDead(target))
        {
            Sacrifice(rune, target);
            return true;
        }

        // WhiteDream - the mindshielded, the faithful and the offering target cannot be converted.
        if (!_mind.TryGetMind(target, out _, out _) || _bloodCultRule.IsTarget(target) ||
            HasComp<BibleUserComponent>(target) || HasComp<MindShieldComponent>(target))
            return TrySacrifice(rune, target, invokersTotal);

        return TryConvert(rune, target, user, invokersTotal);
    }

    private bool TrySacrifice(Entity<CultRuneOfferingComponent> rune, EntityUid target, int invokersAmount)
    {
        if (invokersAmount < rune.Comp.AliveSacrificeInvokersAmount)
        {
            // WhiteDream
            _popup.PopupEntity(
                Loc.GetString("cult-offering-need-invokers",
                    ("current", invokersAmount),
                    ("required", rune.Comp.AliveSacrificeInvokersAmount)),
                rune,
                PopupType.MediumCaution);

            return false;
        }

        Sacrifice(rune, target);
        return true;
    }

    private bool TryConvert(Entity<CultRuneOfferingComponent> rune, EntityUid target, EntityUid user, int invokersTotal)
    {
        if (invokersTotal < rune.Comp.ConvertInvokersAmount)
        {
            _popup.PopupEntity(
                Loc.GetString("cult-offering-need-invokers",
                    ("current", invokersTotal),
                    ("required", rune.Comp.ConvertInvokersAmount)),
                rune,
                PopupType.MediumCaution);

            return false;
        }

        _cultRuneRevive.AddCharges(rune, rune.Comp.ReviveChargesPerOffering);
        Convert(rune, target, user);
        return true;
    }

    private void Sacrifice(Entity<CultRuneOfferingComponent> rune, EntityUid target)
    {
        // Dumont
        _bloodCultRule.MarkOfferingSacrificed(target);

        _cultRuneRevive.AddCharges(rune, rune.Comp.ReviveChargesPerOffering);

        // WhiteDream - the veil takes its due.
        _audio.PlayPvs(rune.Comp.SacrificeSound, rune, AudioParams.Default.WithVolume(2f));

        var transform = Transform(target);

        if (!_mind.TryGetMind(target, out var mindId, out _))
            Spawn(rune.Comp.SoulShardGhostProto, transform.Coordinates);
        else
        {
            var shard = Spawn(rune.Comp.SoulShardProto, transform.Coordinates);
            _mind.TransferTo(mindId, shard);
            _mind.UnVisit(mindId);
            _language.UpdateEntityLanguages(shard);
        }

        _body.GibBody(target); // Dumont
    }

    private void Convert(Entity<CultRuneOfferingComponent> rune, EntityUid target, EntityUid user)
    {
        _bloodCultRule.Convert(target);
        _stun.TryAddStunDuration(target, TimeSpan.FromSeconds(2f));
        if (TryComp(target, out CuffableComponent? cuffs) && cuffs.Container.ContainedEntities.Count >= 1)
        {
            var lastAddedCuffs = cuffs.Container.ContainedEntities[^1];
            _cuffable.Uncuff(target, user, lastAddedCuffs);
        }

        _statusEffects.TryRemoveStatusEffect(target, MutedKey); // Dumont
        _damageable.TryChangeDamage(target, rune.Comp.ConvertHealing);

        _audio.PlayPvs(rune.Comp.ConvertSound, rune, AudioParams.Default.WithVolume(1f));
    }
}
