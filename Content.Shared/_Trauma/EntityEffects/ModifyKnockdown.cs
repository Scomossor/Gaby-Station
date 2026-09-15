// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

public enum StatusEffectModifyType : byte
{
    Update,
    Add,
    Remove,
    Set,
}

/// <summary>
/// Applies knockdown to this entity.
/// </summary>
/// <remarks>
/// </remarks>
public sealed partial class ModifyKnockdown : EventEntityEffect<ModifyKnockdown>
{
    /// <summary>
    /// How long the knockdown applies.
    /// </summary>
    [DataField]
    public TimeSpan? Time = TimeSpan.FromSeconds(2);

    [DataField]
    public StatusEffectModifyType Type = StatusEffectModifyType.Update;

    /// <summary>
    /// Should we drop items when we fall?
    /// </summary>
    [DataField]
    public bool Drop;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class ModifyKnockdownEffectSystem : TraumaEntityEffectSystem<StandingStateComponent, ModifyKnockdown>
{
    [Dependency] private SharedStunSystem _stun = default!;

    protected override void Effect(Entity<StandingStateComponent> ent, ModifyKnockdown effect, EntityEffectBaseArgs args)
    {
        var time = effect.Time;
        switch (effect.Type)
        {
            case StatusEffectModifyType.Update:
                _stun.TryKnockdown(ent.Owner, time, drop: effect.Drop);
                break;
            case StatusEffectModifyType.Add:
                _stun.TryKnockdown(ent.Owner, time, false, drop: effect.Drop);
                break;
            case StatusEffectModifyType.Remove:
                _stun.AddKnockdownTime(ent.Owner, -time ?? TimeSpan.Zero);
                break;
            case StatusEffectModifyType.Set:
                _stun.TryKnockdown(ent.Owner, time, drop: effect.Drop);
                if (TryComp<KnockedDownComponent>(ent, out var knocked))
                    _stun.SetKnockdownTime((ent, knocked), time ?? TimeSpan.Zero);
                break;
        }
    }
}

/// <summary>
/// Applies the paralysis status effect to this entity.
/// </summary>
public sealed partial class ModifyParalysis : EventEntityEffect<ModifyParalysis>
{
    /// <summary>
    /// How long the paralysis applies.
    /// </summary>
    [DataField]
    public TimeSpan? Time = TimeSpan.FromSeconds(2);

    [DataField]
    public StatusEffectModifyType Type = StatusEffectModifyType.Update;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class ModifyParalysisEffectSystem : TraumaEntityEffectSystem<MetaDataComponent, ModifyParalysis>
{
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    protected override void Effect(Entity<MetaDataComponent> ent, ModifyParalysis effect, EntityEffectBaseArgs args)
    {
        var time = effect.Time;
        switch (effect.Type)
        {
            case StatusEffectModifyType.Update:
                _stun.TryUpdateParalyzeDuration(ent, time);
                break;
            case StatusEffectModifyType.Add:
                if (time is { } add)
                    _stun.TryAddParalyzeDuration(ent, add);
                break;
            case StatusEffectModifyType.Remove:
                if (time is { } remove)
                    _status.TryAddTime(ent, SharedStunSystem.StunId, -remove);
                break;
            case StatusEffectModifyType.Set:
                _status.TrySetStatusEffectDuration(ent, SharedStunSystem.StunId, time);
                break;
        }
    }
}

/// <summary>
/// Knocks the target down for <see cref="Time"/> seconds.
/// </summary>
/// <remarks>
/// </remarks>
public sealed partial class Knockdown : EventEntityEffect<Knockdown>
{
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(2);

    [DataField]
    public bool Drop;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class KnockdownEffectSystem : TraumaEntityEffectSystem<StandingStateComponent, Knockdown>
{
    [Dependency] private SharedStunSystem _stun = default!;

    protected override void Effect(Entity<StandingStateComponent> ent, Knockdown effect, EntityEffectBaseArgs args)
    {
        _stun.TryKnockdown(ent.Owner, effect.Time, drop: effect.Drop);
    }
}
