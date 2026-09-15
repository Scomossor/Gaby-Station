// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Components;
using Content.Shared.Rejuvenate;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared._DV.CosmicCult.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._DV.CosmicCult.Abilities;

public sealed partial class CosmicDamageTransferSystem : EntitySystem
{
    [Dependency] private SharedCosmicCultSystem _cult = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private DamageableSystem _damage = default!;

    [SubscribeLocalEvent]
    private void OnTransfer(Entity<CosmicLesserCultistComponent> ent, ref CosmicDamageTransferEvent args)
    {
        if (args.Handled || !_cult.EntityIsCultist(args.Target) || !TryComp<DamageableComponent>(args.Target, out var damageComp))
            return;

        args.Handled = true;

        var damage = damageComp.Damage;
        _damage.TryChangeDamage(ent.Owner, damage, ignoreResistances: true);
        RaiseLocalEvent(args.Target, new RejuvenateEvent());

        _audio.PlayPredicted(ent.Comp.TransferSFX, ent, ent);
        if (_net.IsServer) // Predicted spawn looks bad with animations
            PredictedSpawnAtPosition(ent.Comp.TransferVFX, Transform(args.Target).Coordinates);
    }
}
