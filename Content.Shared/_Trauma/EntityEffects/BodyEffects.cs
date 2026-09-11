// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Gibbing.Events;
using Content.Shared.Gibbing.Systems;
using Content.Shared.Whitelist;
using Content.Trauma.Shared.Body;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.EntityEffects;

/// <summary>
/// Relays entity effects to all body parts of a given type, or all parts. Target must be a body.
/// </summary>
public sealed partial class RelayBodyParts : EventEntityEffect<RelayBodyParts>
{
    /// <summary>
    /// The body part type to run effects on. If null it runs on every part.
    /// </summary>
    [DataField]
    public TraumaBodyPartType? PartType;

    [DataField]
    public BodyPartSymmetry? Symmetry;

    [DataField]
    public LocId? GuidebookText;

    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => GuidebookText is { } key ? Loc.GetString(key, ("chance", Probability)) : null;
}

public sealed partial class RelayBodyPartsEffectSystem : TraumaEntityEffectSystem<BodyComponent, RelayBodyParts>
{
    [Dependency] private TraumaBodySystem _traumaBody = default!;
    [Dependency] private TraumaEntityEffectsSystem _effects = default!;

    private readonly List<EntityUid> _parts = new();

    protected override void Effect(Entity<BodyComponent> ent, RelayBodyParts effect, EntityEffectBaseArgs args)
    {
        _parts.Clear();
        _parts.AddRange(_traumaBody.GetBodyParts(ent, effect.PartType, effect.Symmetry));
        foreach (var part in _parts)
        {
            _effects.ApplyEffects(part, effect.Effects);
        }
    }
}

/// <summary>
/// Relays an effect to a single random body part picked from the allowed types.
/// </summary>
public sealed partial class RelayRandomPart : EventEntityEffect<RelayRandomPart>
{
    [DataField(required: true)]
    public TraumaBodyPartType[] Types = default!;

    [DataField]
    public BodyPartSymmetry? PartSymmetry;

    [DataField("effect", required: true)]
    public EntityEffect Relayed = default!;

    /// <summary>
    /// Effect to apply to the target body if no valid body parts were found.
    /// </summary>
    [DataField]
    public EntityEffect? FailEffect;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Relayed.GuidebookEffectDescription(prototype, entSys);
}

public sealed partial class RelayRandomPartEffectSystem : TraumaEntityEffectSystem<BodyComponent, RelayRandomPart>
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private TraumaBodySystem _traumaBody = default!;
    [Dependency] private TraumaEntityEffectsSystem _effects = default!;

    private readonly List<EntityUid> _parts = new();

    protected override void Effect(Entity<BodyComponent> ent, RelayRandomPart effect, EntityEffectBaseArgs args)
    {
        _parts.Clear();
        foreach (var type in effect.Types)
        {
            _parts.AddRange(_traumaBody.GetBodyParts(ent, type, effect.PartSymmetry));
        }

        if (_parts.Count == 0) // no parts found
        {
            if (effect.FailEffect is { } fail)
                _effects.TryApplyEffect(ent, fail);
            return;
        }

        _effects.TryApplyEffect(_random.Pick(_parts), effect.Relayed);
    }
}

/// <summary>
/// Detaches this target organ or body part from its parent part.
/// </summary>
public sealed partial class DetachOrgan : EventEntityEffect<DetachOrgan>
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class DetachOrganEffectSystem : TraumaEntityEffectSystem<MetaDataComponent, DetachOrgan>
{
    [Dependency] private TraumaBodySystem _traumaBody = default!;

    protected override void Effect(Entity<MetaDataComponent> ent, DetachOrgan effect, EntityEffectBaseArgs args)
    {
        _traumaBody.Detach(ent);
    }
}

/// <summary>
/// Gibs the target mob, body part, or anything gibbable. Whatever it is will be deleted afterwards, be careful.
/// </summary>
public sealed partial class Gib : EventEntityEffect<Gib>
{
    [DataField]
    public bool DropGiblets = true;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class GibEffectSystem : TraumaEntityEffectSystem<TransformComponent, Gib>
{
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private SharedBodySystem _body = default!;
    [Dependency] private TraumaBodySystem _traumaBody = default!;

    protected override void Effect(Entity<TransformComponent> ent, Gib effect, EntityEffectBaseArgs args)
    {
        if (TryComp<BodyComponent>(ent, out var body))
        {
            _body.GibBody(ent, body: body, launchGibs: effect.DropGiblets);
            return;
        }

        if (TryComp<BodyPartComponent>(ent, out var part))
        {
            if (part.Body != null)
                _traumaBody.Detach(ent);
            _body.GibPart(ent, part, launchGibs: effect.DropGiblets);
            return;
        }

        _gibbing.TryGibEntity((ent, ent.Comp), (ent, null), GibType.Gib,
            effect.DropGiblets ? GibContentsOption.Drop : GibContentsOption.Skip, out _);
    }
}

/// <summary>
/// Adds an organ or body part slot to the target entity, which must be a body part.
/// </summary>
public sealed partial class AddOrganSlot : EventEntityEffect<AddOrganSlot>
{
    [DataField(required: true)]
    public string Category = string.Empty;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class AddOrganSlotEffectSystem : TraumaEntityEffectSystem<BodyPartComponent, AddOrganSlot>
{
    [Dependency] private TraumaBodySystem _traumaBody = default!;

    protected override void Effect(Entity<BodyPartComponent> ent, AddOrganSlot effect, EntityEffectBaseArgs args)
    {
        _traumaBody.TryAddSlot(ent, effect.Category);
    }
}

/// <summary>
/// Removes an empty organ or body part slot from the target entity, which must be a body part.
/// </summary>
public sealed partial class RemoveOrganSlot : EventEntityEffect<RemoveOrganSlot>
{
    [DataField(required: true)]
    public string Slot = string.Empty;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class RemoveOrganSlotEffectSystem : TraumaEntityEffectSystem<BodyPartComponent, RemoveOrganSlot>
{
    [Dependency] private TraumaBodySystem _traumaBody = default!;

    protected override void Effect(Entity<BodyPartComponent> ent, RemoveOrganSlot effect, EntityEffectBaseArgs args)
    {
        _traumaBody.TryRemoveSlot(ent, effect.Slot);
    }
}

/// <summary>
/// Moves an organ from one body part to another. The target entity must be the body.
/// </summary>
public sealed partial class MoveOrgan : EventEntityEffect<MoveOrgan>
{
    /// <summary>
    /// The category of the organ to be moved.
    /// </summary>
    [DataField(required: true)]
    public string Organ = string.Empty;

    /// <summary>
    /// The category of the part to move the organ into.
    /// </summary>
    [DataField(required: true)]
    public string Dest = string.Empty;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class MoveOrganEffectSystem : TraumaEntityEffectSystem<BodyComponent, MoveOrgan>
{
    [Dependency] private SharedBodySystem _body = default!;
    [Dependency] private TraumaBodySystem _traumaBody = default!;

    protected override void Effect(Entity<BodyComponent> ent, MoveOrgan effect, EntityEffectBaseArgs args)
    {
        var slot = TraumaBodySystem.SlotId(effect.Organ);
        if (_traumaBody.GetOrgan(ent, effect.Organ) is not { } organ ||
            _traumaBody.GetOrgan(ent, effect.Dest) is not { } dest ||
            !_body.CanInsertOrgan(dest, slot) || // don't remove the original if it couldn't be inserted after
            !_body.RemoveOrgan(organ)) // the organ refused to be removed...
            return;

        if (!_body.InsertOrgan(dest, organ, slot)) // shouldn't fail...
            Log.Error($"Failed to move organ {ToPrettyString(organ)} from {ToPrettyString(ent)} to {ToPrettyString(dest)} in slot {slot}!");
    }
}

/// <summary>
/// Spawns and attaches an organ or body part from the body's initial layout, to this body part entity.
/// </summary>
/// <remarks>
/// </remarks>
public sealed partial class RegenerateOrgan : EventEntityEffect<RegenerateOrgan>
{
    /// <summary>
    /// The category of the slot to regenerate. It must be in this part's initial layout.
    /// </summary>
    [DataField(required: true)]
    public string Slot = string.Empty;

    /// <summary>
    /// Whether to also regenerate child organs.
    /// </summary>
    [DataField]
    public bool Recursive = true;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class RegenerateOrganEffectSystem : TraumaEntityEffectSystem<BodyPartComponent, RegenerateOrgan>
{
    [Dependency] private SharedBodySystem _body = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private TraumaBodySystem _traumaBody = default!;

    protected override void Effect(Entity<BodyPartComponent> ent, RegenerateOrgan effect, EntityEffectBaseArgs args)
    {
        if (!_traumaBody.TryGetPrototypeSlot(ent, out var proto, out var key))
            return;

        var slot = TraumaBodySystem.SlotId(effect.Slot);
        var coords = Transform(ent).Coordinates;

        if (TraumaBodySystem.IsPartCategory(effect.Slot, out _))
        {
            if (!proto.Slots[key].Connections.Contains(slot)
                || !proto.Slots.TryGetValue(slot, out var childSlot)
                || childSlot.Part is not { } partProto
                || !_traumaBody.TryAddSlot(ent, effect.Slot))
                return;

            if (_container.TryGetContainer(ent, SharedBodySystem.GetPartSlotContainerId(slot), out var occupied)
                && occupied.ContainedEntities.Count > 0)
                return; // already there

            var child = Spawn(partProto, coords);
            if (!_body.AttachPart(ent, slot, child, ent.Comp))
            {
                Del(child);
                return;
            }

            foreach (var (organSlot, organProto) in childSlot.Organs)
            {
                if (!_body.TryCreateOrganSlot(child, organSlot, out _))
                    continue;

                if (effect.Recursive)
                    InsertNew(child, organSlot, organProto);
            }
            return;
        }

        if (!proto.Slots[key].Organs.TryGetValue(slot, out var organ)
            || !_traumaBody.TryAddSlot(ent, effect.Slot))
            return;

        InsertNew(ent, slot, organ);
    }

    private void InsertNew(EntityUid part, string slot, string organProto)
    {
        if (!_body.CanInsertOrgan(part, slot))
            return;

        var organ = Spawn(organProto, Transform(part).Coordinates);
        if (!_body.InsertOrgan(part, organ, slot))
            Del(organ); // slot was occupied
    }
}

/// <summary>
/// Spawns and inserts a new organ into the target entity, which must be a body part.
/// </summary>
public sealed partial class InsertNewOrgan : EventEntityEffect<InsertNewOrgan>
{
    /// <summary>
    /// The organ to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Organ;

    [DataField]
    public string? Slot;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

public sealed partial class InsertNewOrganEffectSystem : TraumaEntityEffectSystem<BodyPartComponent, InsertNewOrgan>
{
    [Dependency] private SharedBodySystem _body = default!;

    protected override void Effect(Entity<BodyPartComponent> ent, InsertNewOrgan effect, EntityEffectBaseArgs args)
    {
        if (effect.Slot is { } category && !_body.CanInsertOrgan(ent, TraumaBodySystem.SlotId(category), ent.Comp))
            return;

        var organ = Spawn(effect.Organ, Transform(ent).Coordinates);
        var inserted = effect.Slot is { } cat
            ? _body.InsertOrgan(ent, organ, TraumaBodySystem.SlotId(cat), ent.Comp)
            : _body.AddOrganToFirstValidSlot(ent, organ, ent.Comp);

        if (!inserted)
            Del(organ);
    }
}

/// <summary>
/// Applies effects to a single organ or body part of a given category. The target entity must be the body.
/// </summary>
public sealed partial class RelayOrgan : EventEntityEffect<RelayOrgan>
{
    [DataField(required: true)]
    public string Category = string.Empty;

    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    [DataField]
    public LocId? GuidebookText;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => GuidebookText is { } key ? Loc.GetString(key, ("chance", Probability)) : null;
}

public sealed partial class RelayOrganEffectSystem : TraumaEntityEffectSystem<BodyComponent, RelayOrgan>
{
    [Dependency] private TraumaBodySystem _traumaBody = default!;
    [Dependency] private TraumaEntityEffectsSystem _effects = default!;

    protected override void Effect(Entity<BodyComponent> ent, RelayOrgan effect, EntityEffectBaseArgs args)
    {
        if (_traumaBody.GetOrgan(ent, effect.Category) is { } organ)
            _effects.ApplyEffects(organ, effect.Effects);
    }
}

/// <summary>
/// Applies effects to every internal organ, optionally matching a whitelist. The target entity must be the body.
/// </summary>
public sealed partial class RelayOrgans : EventEntityEffect<RelayOrgans>
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    [DataField]
    public LocId? GuidebookText;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => GuidebookText is { } key ? Loc.GetString(key, ("chance", Probability)) : null;
}

public sealed partial class RelayOrgansEffectSystem : TraumaEntityEffectSystem<BodyComponent, RelayOrgans>
{
    [Dependency] private SharedBodySystem _body = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private TraumaEntityEffectsSystem _effects = default!;

    private readonly List<EntityUid> _organs = new();

    protected override void Effect(Entity<BodyComponent> ent, RelayOrgans effect, EntityEffectBaseArgs args)
    {
        _organs.Clear();
        foreach (var (organ, _) in _body.GetBodyOrgans(ent, ent.Comp))
        {
            if (!_whitelist.IsWhitelistFail(effect.Whitelist, organ))
                _organs.Add(organ);
        }

        foreach (var organ in _organs)
        {
            _effects.ApplyEffects(organ, effect.Effects);
        }
    }
}

/// <summary>
/// Adds a metabolizer type to the target organ entity. Handled on the server, where metabolizers live.
/// </summary>
public sealed partial class AddMetabolizerType : EventEntityEffect<AddMetabolizerType>
{
    [DataField(required: true)]
    public ProtoId<MetabolizerTypePrototype> Type;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}

/// <summary>
/// Removes a metabolizer type from the target organ entity. Handled on the server, where metabolizers live.
/// </summary>
public sealed partial class RemoveMetabolizerType : EventEntityEffect<RemoveMetabolizerType>
{
    [DataField(required: true)]
    public ProtoId<MetabolizerTypePrototype> Type;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;
}
