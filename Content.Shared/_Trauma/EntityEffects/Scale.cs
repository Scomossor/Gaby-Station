// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.EntityEffects;
using Content.Shared.Sprite;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Multiplies the target entity's sprite scale.
/// </summary>
public sealed partial class Scale : EventEntityEffect<Scale>
{
    /// <summary>
    /// What to multiply scale by, componentwise.
    /// Using 1 for an axis means it is left alone.
    /// </summary>
    [DataField(required: true)]
    public Vector2 Multiplier;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class ScaleEffectSystem : TraumaEntityEffectSystem<TransformComponent, Scale>
{
    [Dependency] private SharedScaleVisualsSystem _scale = default!;

    protected override void Effect(Entity<TransformComponent> ent, Scale effect, EntityEffectBaseArgs args)
    {
        var scale = _scale.GetSpriteScale(ent);
        _scale.SetSpriteScale(ent, Vector2.Multiply(scale, effect.Multiplier));
    }
}

/// <summary>
/// Multiplies the target entity's fixture radii.
/// Density is unchanged so mass will increase/decrease.
/// </summary>
public sealed partial class ScaleFixtures : EventEntityEffect<ScaleFixtures>
{
    /// <summary>
    /// What to scale fixtures by.
    /// </summary>
    [DataField(required: true)]
    public float Multiplier;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class ScaleFixturesEffectSystem : TraumaEntityEffectSystem<FixturesComponent, ScaleFixtures>
{
    [Dependency] private SharedPhysicsSystem _physics = default!;

    protected override void Effect(Entity<FixturesComponent> ent, ScaleFixtures effect, EntityEffectBaseArgs args)
    {
        _physics.ScaleFixtures(ent.AsNullable(), effect.Multiplier);
    }
}
