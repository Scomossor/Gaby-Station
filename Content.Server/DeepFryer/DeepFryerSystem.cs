using System;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DeepFryer.Components;
using Content.Shared.DeepFryer.Systems;
using Content.Shared.Storage.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.DeepFryer;

public sealed class DeepFryerSystem : SharedDeepFryerSystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DeepFryerRecipeSystem _recipes = default!;
    [Dependency] private readonly EntityStorageSystem _storage = default!;

    private ProtoId<DamageTypePrototype> damageType = "Heat";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<DeepFryerComponent>();
        while (query.MoveNext(out var fryerUid, out var fryer))
        {
            if (!fryer.Closed)
                continue;

            AddHeatToSolution((fryerUid, fryer), frameTime, fryer.HeatToAddToSolution);

            if (fryer.StoredObjects.Count == 0)
                continue;

            AddHeatDamage((fryerUid, fryer), frameTime);

            if (fryer.FryFinishTime < _timing.CurTime && fryer.FryFinishTime != TimeSpan.Zero)
            {
                if (!TryCookRecipe((fryerUid, fryer)))
                    DeepFryItems((fryerUid, fryer));
            }
        }
    }

    private bool TryCookRecipe(Entity<DeepFryerComponent> fryer)
    {
        if (!_recipes.TryGetRecipe(fryer.Comp.StoredObjects, out var recipe))
            return false;

        if (!TryComp<EntityStorageComponent>(fryer.Owner, out var storage))
            return false;

        var remaining = new Dictionary<EntProtoId, int>(recipe!.Ingredients);

        foreach (var uid in fryer.Comp.StoredObjects.ToArray())
        {
            if (!TryComp<MetaDataComponent>(uid, out var meta))
                continue;

            var proto = meta.EntityPrototype?.ID;

            if (proto == null)
                continue;

            if (!remaining.TryGetValue(proto, out var amount))
                continue;

            QueueDel(uid);

            fryer.Comp.StoredObjects.Remove(uid);

            amount--;

            if (amount <= 0)
                remaining.Remove(proto);
            else
                remaining[proto] = amount;
                
        }

        var result = Spawn(recipe.Result, MapCoordinates.Nullspace);

        _storage.Insert(result, fryer.Owner);

        _popup.PopupPredicted(
            Loc.GetString("deep-fryer-item-cooked"),
            fryer.Owner,
            fryer.Owner);

        fryer.Comp.StoredObjects.Add(result);

        fryer.Comp.FryFinishTime = TimeSpan.Zero;

        return true;
    }

    private void AddHeatToSolution(Entity<DeepFryerComponent> ent, float frameTime, float heatToAdd)
    {
        if (_solution.TryGetSolution(ent.Owner,
                ent.Comp.FryerSolutionContainer,
                out var solutionRef,
                out var solution)) // Dumont
        {
            solution.Temperature = Math.Clamp(solution.Temperature + (heatToAdd * frameTime), 293f, ent.Comp.MaxHeat);


            _solution.UpdateChemicals(solutionRef.Value);
            // Dumont End
        }
    }

    private void AddHeatDamage(Entity<DeepFryerComponent> ent, float frameTime)
    {
        var heatProto = _prototypeManager.Index(damageType);

        foreach (var entity in ent.Comp.StoredObjects)
        {
            if (!TryComp<DamageableComponent>(entity, out _))
                continue;

            _damageable.TryChangeDamage(entity, new DamageSpecifier(heatProto, ent.Comp.HeatDamage * frameTime));
        }
    }
}