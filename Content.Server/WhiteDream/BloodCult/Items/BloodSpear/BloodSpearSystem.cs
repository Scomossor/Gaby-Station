// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Actions;
using Content.Server.Hands.Systems;
using Content.Server.Projectiles;
using Content.Shared.Humanoid;
using Content.Shared.Item;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Content.Shared.WhiteDream.BloodCult.Spells;
using Robust.Server.Audio;

namespace Content.Server.WhiteDream.BloodCult.Items.BloodSpear;

public sealed partial class BloodSpearSystem : EntitySystem
{
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private ProjectileSystem _projectiles = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BloodSpearComponent, EmbedEvent>(OnEmbed);

        SubscribeLocalEvent<BloodSpearComponent, GettingPickedUpAttemptEvent>(OnPickedUp);
        SubscribeLocalEvent<BloodCultistComponent, BloodSpearRecalledEvent>(OnSpearRecalled);

        SubscribeLocalEvent<BloodSpearComponent, ComponentShutdown>(OnComponentShutdown);
    }

    private void OnComponentShutdown(Entity<BloodSpearComponent> spear, ref ComponentShutdown args)
    {
        if (!spear.Comp.RecallAction.HasValue)
            return;

        _actions.RemoveAction(spear.Comp.RecallAction);
    }

    private void OnEmbed(Entity<BloodSpearComponent> spear, ref EmbedEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.Embedded))
            return;

        _stun.TryAddParalyzeDuration(args.Embedded, spear.Comp.ParalyzeTime);
    }

    private void OnPickedUp(Entity<BloodSpearComponent> spear, ref GettingPickedUpAttemptEvent args)
    {
        if (args.Cancelled || spear.Comp.Master.HasValue ||
            !TryComp(args.User, out BloodCultistComponent? bloodCultist))
            return;

        spear.Comp.Master = args.User;

        var action = _actions.AddAction(args.User, spear.Comp.RecallActionId);
        spear.Comp.RecallAction = action;
        bloodCultist.BloodSpear = spear;
    }

    private void OnSpearRecalled(Entity<BloodCultistComponent> cultist, ref BloodSpearRecalledEvent args)
    {
        if (args.Handled)
            return;

        var spearUid = cultist.Comp.BloodSpear;
        if (!spearUid.HasValue || !TryComp(spearUid, out BloodSpearComponent? spear))
            return;

        // A thrown halberd remains embedded in its victim. Detach it through the projectile system
        // before moving it into the master's hand so the embedded-container bookkeeping is cleared.
        if (TryComp(spearUid, out EmbeddableProjectileComponent? embeddable))
            _projectiles.EmbedDetach(spearUid.Value, embeddable, cultist);

        if (!_hands.TryForcePickupAnyHand(cultist, spearUid.Value))
            return;

        _audio.PlayPvs(spear.RecallAudio, spearUid.Value);
        args.Handled = true;
    }

    public void DetachSpearFromMaster(Entity<BloodCultistComponent> cultist)
    {
        if (cultist.Comp.BloodSpear is not { } spearUid || !TryComp(spearUid, out BloodSpearComponent? spear))
            return;

        _actions.RemoveAction(spear.RecallAction);
        spear.RecallAction = null;
        spear.Master = null;
        cultist.Comp.BloodSpear = null;
    }
}
