// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Spawns a number of entities of a given prototype at the coordinates of this entity.
/// </summary>
public sealed partial class SpawnEntity : EventEntityEffect<SpawnEntity>
{
    /// <summary>
    /// Amount of entities we're spawning.
    /// </summary>
    [DataField]
    public int Number = 1;

    /// <summary>
    /// Prototype of the entity we're spawning.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Entity;

    [DataField]
    public bool Predicted = true;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class SpawnEntityEffectSystem : TraumaEntityEffectSystem<TransformComponent, SpawnEntity>
{
    [Dependency] private INetManager _net = default!;

    protected override void Effect(Entity<TransformComponent> ent, SpawnEntity effect, EntityEffectBaseArgs args)
    {
        if (!_net.IsServer)
            return;

        var scale = args is EntityEffectReagentArgs reagent ? reagent.Scale.Float() : 1f;
        var quantity = effect.Number * (int) Math.Floor(scale);
        for (var i = 0; i < quantity; i++)
        {
            SpawnNextToOrDrop(effect.Entity, ent, ent.Comp);
        }
    }
}
