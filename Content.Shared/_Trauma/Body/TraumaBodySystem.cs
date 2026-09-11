// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.Body;

public enum TraumaBodyPartType : byte
{
    Other,
    Torso,
    Chest,
    Groin,
    Head,
    Arm,
    Hand,
    Leg,
    Foot,
    Tail,
}

public sealed partial class TraumaBodySystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedBodySystem _body = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    private static readonly Dictionary<string, BodyPartType> PartCategories = new()
    {
        ["Torso"] = BodyPartType.Chest,
        ["Chest"] = BodyPartType.Chest,
        ["Groin"] = BodyPartType.Groin,
        ["Head"] = BodyPartType.Head,
        ["Arm"] = BodyPartType.Arm,
        ["Hand"] = BodyPartType.Hand,
        ["Leg"] = BodyPartType.Leg,
        ["Foot"] = BodyPartType.Foot,
    };

    public static BodyPartType ToPartType(TraumaBodyPartType type) => type switch
    {
        TraumaBodyPartType.Torso or TraumaBodyPartType.Chest => BodyPartType.Chest,
        TraumaBodyPartType.Groin => BodyPartType.Groin,
        TraumaBodyPartType.Head => BodyPartType.Head,
        TraumaBodyPartType.Arm => BodyPartType.Arm,
        TraumaBodyPartType.Hand => BodyPartType.Hand,
        TraumaBodyPartType.Leg => BodyPartType.Leg,
        TraumaBodyPartType.Foot => BodyPartType.Foot,
        TraumaBodyPartType.Tail => BodyPartType.Tail,
        _ => BodyPartType.Other,
    };

    public static string SlotId(string category) => category.ToLowerInvariant();

    public static bool IsPartCategory(string category, out BodyPartType type)
        => PartCategories.TryGetValue(category, out type);

    public IEnumerable<EntityUid> GetBodyParts(EntityUid body, TraumaBodyPartType? type, BodyPartSymmetry? symmetry)
    {
        if (!TryComp<BodyComponent>(body, out var comp))
            yield break;

        if (type is not { } t)
        {
            foreach (var (part, partComp) in _body.GetBodyChildren(body, comp))
            {
                if (symmetry == null || partComp.Symmetry == symmetry)
                    yield return part;
            }
            yield break;
        }

        foreach (var (part, _) in _body.GetBodyChildrenOfType(body, ToPartType(t), comp, symmetry))
        {
            yield return part;
        }
    }

    /// <summary>
    /// categoria for de parte, cai na primeira parte daquele tipo.
    /// </summary>
    public EntityUid? GetOrgan(EntityUid body, string category)
    {
        var slot = SlotId(category);
        foreach (var (organ, organComp) in _body.GetBodyOrgans(body))
        {
            if (organComp.SlotId == slot)
                return organ;
        }

        if (!IsPartCategory(category, out var type) || !TryComp<BodyComponent>(body, out var comp))
            return null;

        foreach (var (part, _) in _body.GetBodyChildrenOfType(body, type, comp))
        {
            return part;
        }

        return null;
    }

    public bool HasSlot(Entity<BodyPartComponent> part, string category)
    {
        var slot = SlotId(category);
        return IsPartCategory(category, out _)
            ? part.Comp.Children.ContainsKey(slot)
            : part.Comp.Organs.ContainsKey(slot);
    }

    public bool TryAddSlot(Entity<BodyPartComponent> part, string category)
    {
        var slot = SlotId(category);
        if (HasSlot(part, category))
            return true;

        return IsPartCategory(category, out var type)
            ? _body.TryCreatePartSlot(part, slot, type, BodyPartSymmetry.None, out _, part.Comp)
            : _body.TryCreateOrganSlot(part, slot, out _, part.Comp);
    }

    public bool TryRemoveSlot(Entity<BodyPartComponent> part, string category)
    {
        var slot = SlotId(category);
        var isPart = IsPartCategory(category, out _);
        if (isPart ? !part.Comp.Children.ContainsKey(slot) : !part.Comp.Organs.ContainsKey(slot))
            return false;

        var containerId = isPart
            ? SharedBodySystem.GetPartSlotContainerId(slot)
            : SharedBodySystem.GetOrganContainerId(slot);

        if (_container.TryGetContainer(part, containerId, out var container))
        {
            if (container.ContainedEntities.Count > 0)
                return false;

            _container.ShutdownContainer(container);
        }

        if (isPart)
            part.Comp.Children.Remove(slot);
        else
            part.Comp.Organs.Remove(slot);

        Dirty(part);
        return true;
    }

    public bool Detach(EntityUid uid)
    {
        if (HasComp<BodyPartComponent>(uid))
        {
            if (_body.GetParentPartAndSlotOrNull(uid) is not { } parent)
                return false;

            return _body.DetachPart(parent.Parent, parent.Slot, uid);
        }

        return TryComp<OrganComponent>(uid, out var organ) && _body.RemoveOrgan(uid, organ);
    }

    public bool TryGetPrototypeSlot(Entity<BodyPartComponent> part, [NotNullWhen(true)] out BodyPrototype? proto, [NotNullWhen(true)] out string? key)
    {
        proto = null;
        key = null;
        if (part.Comp.Body is not { } body
            || !TryComp<BodyComponent>(body, out var bodyComp)
            || bodyComp.Prototype is not { } protoId
            || !_proto.TryIndex(protoId, out proto))
            return false;

        key = _body.GetParentPartAndSlotOrNull(part) is { } parent ? parent.Slot : proto.Root;
        return proto.Slots.ContainsKey(key);
    }
}
