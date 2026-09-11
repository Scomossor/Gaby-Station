// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Standing;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Tries to make the target stand, or downs the target.
/// </summary>
public sealed partial class SetStanding : EventEntityEffect<SetStanding>
{
    /// <summary>
    /// Whether to stand or down the target.
    /// </summary>
    [DataField]
    public bool Standing = true;

    /// <summary>
    /// Force the target to stand/be downed.
    /// </summary>
    [DataField]
    public bool Force;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class SetStandingEffectSystem : TraumaEntityEffectSystem<StandingStateComponent, SetStanding>
{
    [Dependency] private StandingStateSystem _standing = default!;

    protected override void Effect(Entity<StandingStateComponent> ent, SetStanding effect, EntityEffectBaseArgs args)
    {
        if (effect.Standing)
            _standing.Stand(ent, ent.Comp, force: effect.Force);
        else
            _standing.Down(ent, force: effect.Force, standingState: ent.Comp);
    }
}
