// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Timing;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Effect that starts a use delay on the target entity.
/// </summary>
public sealed partial class StartUseDelay : EventEntityEffect<StartUseDelay>
{
    /// <summary>
    /// The specific use delay to start.
    /// </summary>
    [DataField]
    public string DelayId = UseDelaySystem.DefaultId;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class StartUseDelayEffectSystem : TraumaEntityEffectSystem<UseDelayComponent, StartUseDelay>
{
    [Dependency] private UseDelaySystem _useDelay = default!;

    protected override void Effect(Entity<UseDelayComponent> ent, StartUseDelay effect, EntityEffectBaseArgs args)
    {
        _useDelay.TryResetDelay(ent, id: effect.DelayId);
    }
}
