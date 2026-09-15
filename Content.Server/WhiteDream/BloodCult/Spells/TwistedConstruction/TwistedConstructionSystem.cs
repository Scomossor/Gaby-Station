// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.DoAfter;
using Content.Server.Mind;
using Content.Server.Stack;
using Content.Server.WhiteDream.BloodCult.Components;
using Content.Shared.DoAfter;
using Content.Shared.Materials;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Content.Shared.WhiteDream.BloodCult.Spells;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.WhiteDream.BloodCult.Spells.TwistedConstruction;

public sealed partial class TwistedConstructionSystem : EntitySystem
{
    [Dependency] private DoAfterSystem _doAfter = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private TransformSystem _transform = default!;
    [Dependency] private StackSystem _stack = default!;
    [Dependency] private TagSystem _tag = default!;

    private static readonly ProtoId<MaterialPrototype> PlasteelMaterial = "Plasteel";
    private static readonly ProtoId<TagPrototype> TwistedConstructionTargetTag = "TwistedConstructionTarget";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhysicalCompositionComponent, MapInitEvent>(OnMaterialMapInit);
        SubscribeLocalEvent<BloodCultTwistedConstructionEvent>(OnTwistedConstruction);
        SubscribeLocalEvent<TwistedConstructionTargetComponent, TwistedConstructionDoAfterEvent>(OnDoAfter);
    }

    private void OnMaterialMapInit(Entity<PhysicalCompositionComponent> ent, ref MapInitEvent args)
    {
        if (HasComp<TwistedConstructionTargetComponent>(ent) || !HasComp<StackComponent>(ent))
            return;

        // Dumont
        if (!ent.Comp.MaterialComposition.ContainsKey(PlasteelMaterial))
            return;

        var twisted = EnsureComp<TwistedConstructionTargetComponent>(ent);
        twisted.ReplacementProto = "RunedMetal";
        _tag.AddTag(ent, TwistedConstructionTargetTag);
    }

    private void OnTwistedConstruction(BloodCultTwistedConstructionEvent ev)
    {
        if (ev.Handled || !TryComp(ev.Target, out TwistedConstructionTargetComponent? twistedConstruction))
            return;

        var args = new DoAfterArgs(EntityManager,
            ev.Performer,
            twistedConstruction.DoAfterDelay,
            new TwistedConstructionDoAfterEvent(),
            ev.Target);

        if (_doAfter.TryStartDoAfter(args))
            ev.Handled = true;
    }

    private void OnDoAfter(Entity<TwistedConstructionTargetComponent> target, ref TwistedConstructionDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var replacement = Spawn(target.Comp.ReplacementProto, _transform.GetMapCoordinates(target));
        if (TryComp(target, out StackComponent? stack) && TryComp(replacement, out StackComponent? targetStack))
            _stack.SetCount(replacement, stack.Count, targetStack);

        if (_mind.TryGetMind(target, out var mindId, out _))
            _mind.TransferTo(mindId, replacement);

        QueueDel(target);
    }
}
