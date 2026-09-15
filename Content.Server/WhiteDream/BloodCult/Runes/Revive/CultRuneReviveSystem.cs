// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Rejuvenate;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server.WhiteDream.BloodCult.Runes.Revive;

public sealed partial class CultRuneReviveSystem : EntitySystem
{
    [Dependency] private EuiManager _eui = default!;

    [Dependency] private CultRuneBaseSystem _cultRune = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultRuneReviveComponent, TryInvokeCultRuneEvent>(OnReviveRuneInvoked);
    }

    private void OnReviveRuneInvoked(Entity<CultRuneReviveComponent> ent, ref TryInvokeCultRuneEvent args)
    {
        var chargesProvider = EnsureReviveRuneChargesProvider(ent);
        if (chargesProvider is null)
        {
            _popup.PopupEntity(Loc.GetString("cult-revive-rune-no-charges"), args.User, args.User);
            args.Cancel();
            return;
        }

        if (chargesProvider.Charges <= 0)
        {
            _popup.PopupEntity(Loc.GetString("cult-revive-rune-no-charges"), args.User, args.User);
            args.Cancel();
            return;
        }

        var possibleTargets = _cultRune.GetTargetsNearRune(ent,
            ent.Comp.ReviveRange,
            entity =>
                !HasComp<BloodCultistComponent>(entity) ||
                !HasComp<DamageableComponent>(entity) ||
                !HasComp<MobThresholdsComponent>(entity) ||
                !HasComp<MobStateComponent>(entity) ||
                !_mobState.IsDead(entity) ||
                !TryGetReviveMind(entity, out _)
        );

        if (possibleTargets.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("cult-rune-no-targets"), args.User, args.User);
            args.Cancel();
            return;
        }

        // A rune can have more than one corpse on it. Select the one closest to the rune instead
        // of relying on HashSet iteration order, which changes unpredictably.
        var runePosition = _transform.GetMapCoordinates(ent);
        var victim = possibleTargets
            .OrderBy(entity => (_transform.GetMapCoordinates(entity).Position - runePosition.Position).LengthSquared())
            .First();

        if (!TryGetReviveMind(victim, out var mind))
        {
            _popup.PopupEntity(Loc.GetString("cult-rune-no-targets"), args.User, args.User);
            args.Cancel();
            return;
        }

        Revive(victim, mind, ent);
    }

    public void AddCharges(EntityUid ent, int charges)
    {
        var chargesProvider = EnsureReviveRuneChargesProvider(ent);
        if (chargesProvider is null)
            return;

        chargesProvider.Charges += charges;
    }

    private void Revive(EntityUid target, MindComponent mind, Entity<CultRuneReviveComponent> rune)
    {
        var chargesProvider = EnsureReviveRuneChargesProvider(rune);
        if (chargesProvider is null || chargesProvider.Charges <= 0)
            return;

        chargesProvider.Charges--;

        // Dumont changes start
        RaiseLocalEvent(target, new RejuvenateEvent(false, false));
        // Dumont end

        if (mind.CurrentEntity == target ||
            !_player.TryGetSessionById(mind.UserId, out var playerSession))
            return;

        _eui.OpenEui(new ReturnToBodyEui(mind, _mind, _player), playerSession);
    }

    /// <summary>
    ///     Finds a player mind that can actually return to this cultist. Mindless corpses are not
    ///     valid targets and therefore cannot consume a revival charge.
    /// </summary>
    private bool TryGetReviveMind(EntityUid target, out MindComponent mind)
    {
        if (_mind.TryGetMind(target, out _, out var currentMind) &&
            currentMind is { UserId: not null })
        {
            mind = currentMind;
            return true;
        }

        if (TryComp<BloodCultistComponent>(target, out var cultist) &&
            cultist.OriginalMind is { } originalMind &&
            !TerminatingOrDeleted(originalMind.Owner) &&
            originalMind.Comp.UserId is not null)
        {
            mind = originalMind.Comp;
            return true;
        }

        mind = default!;
        return false;
    }

    private ReviveRuneChargesProviderComponent? EnsureReviveRuneChargesProvider(EntityUid ent)
    {
        var mapUid = Transform(ent).MapUid;
        return !mapUid.HasValue ? null : EnsureComp<ReviveRuneChargesProviderComponent>(mapUid.Value);
    }
}
