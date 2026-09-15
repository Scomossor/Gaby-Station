// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Throws the target entity in a random direction, with a fixed speed.
/// </summary>
public sealed partial class ThrowRandomly : EventEntityEffect<ThrowRandomly>
{
    /// <summary>
    /// The speed at which the thrown entity will be thrown.
    /// </summary>
    [DataField]
    public float Speed = 10f;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class ThrowRandomlyEffectSystem : TraumaEntityEffectSystem<MetaDataComponent, ThrowRandomly>
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ThrowingSystem _throwing = default!;

    protected override void Effect(Entity<MetaDataComponent> ent, ThrowRandomly effect, EntityEffectBaseArgs args)
    {
        var direction = _random.NextAngle().ToVec();
        _throwing.TryThrow(ent, direction, baseThrowSpeed: effect.Speed);
    }
}
