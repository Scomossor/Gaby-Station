// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Standing;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Makes the target drop the items they are holding.
/// </summary>
public sealed partial class DropItems : EventEntityEffect<DropItems>
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class DropItemsEffectSystem : TraumaEntityEffectSystem<HandsComponent, DropItems>
{
    protected override void Effect(Entity<HandsComponent> ent, DropItems effect, EntityEffectBaseArgs args)
    {
        var ev = new DropHandItemsEvent();
        RaiseLocalEvent(ent, ref ev);
    }
}

/// <summary>
/// Makes the target drop a random item, then run entity effects on it.
/// </summary>
public sealed partial class DropRandomItem : EventEntityEffect<DropRandomItem>
{
    /// <summary>
    /// Effects to run on the item after dropping it.
    /// </summary>
    [DataField]
    public EntityEffect[] Effects = [];

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class DropRandomItemEffectSystem : TraumaEntityEffectSystem<HandsComponent, DropRandomItem>
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private TraumaEntityEffectsSystem _effects = default!;

    private readonly List<EntityUid> _items = new();

    protected override void Effect(Entity<HandsComponent> ent, DropRandomItem effect, EntityEffectBaseArgs args)
    {
        _items.Clear();
        foreach (var held in _hands.EnumerateHeld(ent.AsNullable()))
        {
            _items.Add(held);
        }

        if (_items.Count == 0)
            return;

        var item = _random.Pick(_items);
        if (!_hands.TryDrop(ent.AsNullable(), item)) // glued etc
            return;

        _effects.ApplyEffects(item, effect.Effects);
    }
}
