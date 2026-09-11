// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Relays an effect to every entity in some radius, matching some conditions.
/// Does not apply it to this effect's target entity.
/// </summary>
public sealed partial class RelayNearby : EventEntityEffect<RelayNearby>
{
    /// <summary>
    /// The effect to apply to found entities.
    /// </summary>
    /// <remarks>
    /// </remarks>
    [DataField("effect", required: true)]
    public EntityEffect Relayed = default!;

    /// <summary>
    /// The component to use for lookups.
    /// If this is Transform it will find any entity in range.
    /// Use the rarest component you can for best performance.
    /// You don't need to include this in <see cref="Whitelist"/>.
    /// </summary>
    [DataField(required: true)]
    public string CompName = string.Empty;

    /// <summary>
    /// Radius to search around the target entity.
    /// </summary>
    [DataField]
    public float Range = 5f;

    /// <summary>
    /// Flags to use for lookups.
    /// </summary>
    [DataField]
    public LookupFlags Flags = LookupFlags.All;

    /// <summary>
    /// If non-null, found entities must also match this whitelist.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// If non-null, found entities cannot match this blacklist.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Relayed.GuidebookEffectDescription(prototype, entSys);
}

public sealed partial class RelayNearbyEffectSystem : TraumaEntityEffectSystem<TransformComponent, RelayNearby>
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TraumaEntityEffectsSystem _effects = default!;

    private readonly HashSet<Entity<IComponent>> _found = new();

    protected override void Effect(Entity<TransformComponent> ent, RelayNearby effect, EntityEffectBaseArgs args)
    {
        var type = Factory.GetRegistration(effect.CompName).Type;
        var coords = _transform.GetMapCoordinates(ent, ent.Comp);

        _found.Clear();
        _lookup.GetEntitiesInRange(type, coords, effect.Range, _found, effect.Flags);
        foreach (var found in _found)
        {
            var uid = found.Owner;
            if (uid == ent.Owner) // don't apply to itself
                continue;

            if (!_whitelist.CheckBoth(uid, blacklist: effect.Blacklist, whitelist: effect.Whitelist))
                continue;

            _effects.TryApplyEffect(uid, effect.Relayed);
        }
    }
}
