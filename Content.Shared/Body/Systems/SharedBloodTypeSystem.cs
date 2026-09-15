using Content.Shared.Chemistry.EntitySystems;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components;
using Content.Shared._Shitmed.Damage;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Damage;
using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Components;
using Content.Shared.Random.Helpers;
using Content.Shared.Humanoid;
using Content.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Shared.Body.Systems;

public abstract partial class SharedBloodTypeSystem : EntitySystem
{

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] protected readonly SharedSolutionContainerSystem SolutionContainer = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    /// <summary>
    /// Generates a random blood type for the entity based on its species. If the species does not have a defined blood type, it defaults to human blood types
    /// and if the entity has a BloodTypeComponent, it will not generate a new blood type and will return the existing blood type.
    /// </summary>
    /// <param name="uidAppearence"> The humanoid appearance component of the entity. </param>
    /// <returns></returns>
    public string GenerateBloodType(HumanoidAppearanceComponent? uidAppearence)
    {
        ProtoId<WeightedRandomPrototype> bloodTypesWeights = uidAppearence!.Species.Id + "Types";
        try
        {
            return SharedRandomExtensions.Pick(_prototypeManager.Index(bloodTypesWeights), _random);
        }
        catch (Exception e)
        {
            bloodTypesWeights = "HumanTypes";
            return SharedRandomExtensions.Pick(_prototypeManager.Index(bloodTypesWeights), _random);
        }
    }

    ///<summary>
    /// Returns the blood type of the entity if it has a BloodTypeComponent, otherwise returns null.
    ///</summary>
    /// <param name="uid">The entity to check for a blood type.</param>
    /// <returns>The blood type of the entity, or null if it does not have a BloodTypeComponent.</returns>
    public ProtoId<BloodTypePrototype>? GetBloodType(EntityUid uid)
    {
        if (!TryComp(uid, out BloodTypeComponent? comp))
            return null;
        return comp.Type;
    }

    /// <summary>
    /// Sets the blood type of the entity bloodTypeComponent to a random blood type
    /// based on its species, if it does not already have a blood type.
    /// </summary>
    /// <param name="uid">The entity to set the blood type for.</param>
    /// <param name="uidBloodType">The blood type component of the entity.</param>
    /// <param name="uidAppearence">The humanoid appearance component of the entity.</param>
    public void SetBloodType(EntityUid uid, BloodTypeComponent? uidBloodType = null, HumanoidAppearanceComponent? uidAppearence = null)
    {
        if (!Resolve(uid, ref uidBloodType))
            return;
        if (uidBloodType.Type is not null)
            return;
        if (!Resolve(uid, ref uidAppearence, false))
            return;
        uidBloodType.Type = GenerateBloodType(uidAppearence);
    }

    /// <summary>
    /// Applies damage to the entity based on the amount of foreign blood in its bloodstream.
    /// The damage is calculated based on the incompatibility damage of the entity's
    /// blood type and the amount of foreign blood in its bloodstream.
    /// The foreign blood is removed from the bloodstream after the damage is applied.
    /// </summary>
    /// <param name="uid">The entity to apply damage to.</param>
    /// <param name="uidBloodType">The blood type component of the entity.</param>
    public void ApplyBloodTypeDamage(EntityUid uid, BloodTypeComponent? uidBloodType = null)
    {
        if (!Resolve(uid, ref uidBloodType))
            return;
        if (!TryComp<BloodstreamComponent>(uid, out var bloodstream))
            return;
        if (_prototypeManager.Index(uidBloodType.Type) is null || uidBloodType.Type is null)
            return;
        if (!SolutionContainer.ResolveSolution(uid, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution,out var solution))
            return;
        var damage = _prototypeManager!.Index(uidBloodType.Type).IncompatibilityDamage;
        FixedPoint2 foreignAmount = GetForeignBloodAmount(uid, solution);
        if (foreignAmount > 0)
        {
            damage = damage * 10f * (foreignAmount / bloodstream.BloodMaxVolume);
            _damageableSystem.TryChangeDamage(uid, damage, ignoreResistances: false,
            interruptsDoAfters: false, splitDamage: SplitDamageBehavior.SplitEnsureAll,
            targetPart: TargetBodyPart.All);
            var internalBlood = GetForeignBloodList(uidBloodType, solution);
            internalBlood.ForEach(content => solution.RemoveReagent(content.Reagent, uidBloodType.ForeignBloodDeducted));
        }
    }

    /// <summary>
    /// Returns a list of all foreign blood in the entity's bloodstream that
    ///  is incompatible with its blood type.
    /// </summary>
    /// <param name="comp">The blood type component of the entity.</param>
    /// <param name="soln">The solution representing the bloodstream.</param>
    /// <returns>A list of foreign blood reagents.</returns>
    public List<ReagentQuantity> GetForeignBloodList(BloodTypeComponent comp, in Solution? soln)
    {
        List<ReagentQuantity> aux = new List<ReagentQuantity>();
        if (soln is null)
            return aux;
        var type = _prototypeManager!.Index(comp.Type);
        foreach (var content in soln.Contents)
        {
            foreach (var data in content.Reagent.EnsureReagentData())
            {
                if (data is BloodTypeData)
                    if (!type!.Compatibilities!.Contains(((BloodTypeData) data)?.Type ?? "N/A"))
                        aux.Add(content);
            }
        }
        return aux;
    }

    /// <summary>
    /// Returns the total amount of foreign blood in the entity's bloodstream that is
    /// incompatible with its blood type.
    /// </summary>
    /// <param name="uid">The entity uid.</param>
    /// <param name="soln">The solution representing the bloodstream.</param>
    /// <returns>The total amount of foreign blood.</returns>
    public FixedPoint2 GetForeignBloodAmount(EntityUid uid, in Solution? soln)
    {
        FixedPoint2 amount = FixedPoint2.Zero;
        if (!TryComp(uid, out BloodTypeComponent? bloodTypeComp))
            return amount;
        if (soln is null || bloodTypeComp.Type is null)
            return amount;
        var type = _prototypeManager!.Index(bloodTypeComp.Type);
        foreach (var internalContent in soln!.Contents)
        {
            foreach (var data in internalContent.Reagent.EnsureReagentData())
            {
                if (data is BloodTypeData)
                {
                    if (type!.Compatibilities!.Contains(((BloodTypeData) data)?.Type ?? "N/A"))
                        continue;
                    amount += internalContent.Quantity;
                }
            }
        }
        return amount;
    }

    /// <summary>
    /// Sets the blood type data for the reagents in the solution to the blood type
    /// of the entity.
    /// </summary>
    /// <param name="uid">The entity uid.</param>
    /// <param name="soln">The solution representing the bloodstream.</param>
    public void SetBloodData(EntityUid uid, ref Solution soln)
    {
        if (!TryComp(uid, out BloodTypeComponent? bloodTypeComp))
            return;
        BloodTypeData typeData = new BloodTypeData()
        {
            Type = bloodTypeComp.Type
        };
        foreach (var internalContent in soln.Contents)
        {
            var data = internalContent.Reagent.EnsureReagentData();
            if (!data.Exists(aux => aux is BloodTypeData))
                data.Add(typeData);
        }
    }

    /// <summary>
    /// Sets the blood type data for the reagents in the solution to the BloodComponent type
    /// of the entity.
    /// </summary>
    /// <param name="type">The blood type.</param>
    /// <param name="soln">The solution representing the internal reagent storage of the entity.</param>
    public void SetBloodData(ProtoId<BloodTypePrototype>? type, ref Solution soln)
    {
        BloodTypeData typeData = new BloodTypeData()
        {
            Type = type
        };
        foreach (var internalContent in soln!.Contents)
        {
            var data = internalContent.Reagent.EnsureReagentData();
            if (!data.Exists(aux => aux is BloodTypeData))
                data.Add(typeData);
        }
    }

    /// <summary>
    /// Checks if the solution contains any reagents with blood type data.
    /// </summary>
    /// <param name="soln">The solution to check.</param>
    /// <returns>True if the solution contains blood type data, false otherwise.</returns>
    public bool IsBloodDataSet(in Solution? soln)
    {
        if (soln is null)
            return false;
        bool aux = false;
        foreach (var internalContent in soln!.Contents)
        {
            var data = internalContent.Reagent.EnsureReagentData();
            if (!data.Exists(aux => aux is BloodTypeData) || data is null)
                continue;
            aux = true;
        }
        return aux;
    }
}
