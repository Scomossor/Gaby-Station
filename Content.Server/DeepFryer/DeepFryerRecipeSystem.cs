using Content.Shared.DeepFryer;
using Content.Shared.DeepFryer.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.DeepFryer;

public sealed class DeepFryerRecipeSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public bool TryGetRecipe(
        List<EntityUid> entities,
        out DeepFryerRecipePrototype? recipe)
    {
        recipe = null;

        var found = new Dictionary<string, int>();

        foreach (var uid in entities)
        {
            if (!TryComp<MetaDataComponent>(uid, out var meta))
                continue;

            var proto = meta.EntityPrototype?.ID;

            if (proto == null)
                continue;

            found.TryAdd(proto, 0);
            found[proto]++;
        }

        foreach (var r in _prototype.EnumeratePrototypes<DeepFryerRecipePrototype>())
        {
            var ok = true;

            foreach (var ingredient in r.Ingredients)
            {
                if (!found.TryGetValue(ingredient.Key, out var amount))
                {
                    ok = false;
                    break;
                }

                if (amount < ingredient.Value)
                {
                    ok = false;
                    break;
                }
            }

            if (ok)
            {
                recipe = r;
                return true;
            }
        }

        return false;
    }
}