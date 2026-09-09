using System.Linq;
using Content.Server.Humanoid;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Genetics;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Genetics;

public sealed class MorphismGenSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MarkingManager _markings = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MorphismGenComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MorphismGenComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<MorphismGenComponent, MorphismGenActionEvent>(OnMorphismAction);

        SubscribeNetworkEvent<MorphismChangeMarkingEvent>(OnChangeMarking);
        SubscribeNetworkEvent<MorphismChangeMarkingColorEvent>(OnChangeMarkingColor);
        SubscribeNetworkEvent<MorphismSlotEvent>(OnSlotChange);
        SubscribeNetworkEvent<MorphismChangeBodyColorEvent>(OnBodyColorChange);
    }

    private void OnInit(Entity<MorphismGenComponent> ent, ref ComponentInit args)
        => ent.Comp.ActionEntity = _action.AddAction(ent, ent.Comp.ActionId);

    private void OnShutdown(Entity<MorphismGenComponent> ent, ref ComponentShutdown args)
        => _action.RemoveAction(ent.Comp.ActionEntity);

    private void OnMorphismAction(Entity<MorphismGenComponent> ent, ref MorphismGenActionEvent args)
    {
        if (args.Handled) return;
        args.Handled = true;
        SendState(args.Performer, args.Performer);
    }

    private void SendState(EntityUid target, EntityUid receiver)
    {
        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoid))
            return;

        var ev = new MorphismMenuOpenedEvent(
            GetNetEntity(target),
            humanoid.Species,
            humanoid.SkinColor,
            humanoid.EyeColor,
            humanoid.MarkingSet
        );

        RaiseNetworkEvent(ev, receiver);
    }

    private bool IsAuthorized(NetEntity netTarget, EntitySessionEventArgs session,
        out Entity<HumanoidAppearanceComponent> target)
    {
        target = default;

        if (session.SenderSession.AttachedEntity is not { } user)
            return false;

        if (GetEntity(netTarget) != user)
            return false;

        if (!TryComp<MorphismGenComponent>(user, out var morphism) || morphism.ActionEntity == null)
            return false;

        if (!_actionBlocker.CanInteract(user, null))
            return false;

        if (!TryComp<HumanoidAppearanceComponent>(user, out var humanoid))
            return false;

        target = (user, humanoid);
        return true;
    }

    private static bool IsKnownCategory(MarkingCategories category)
        => Enum.IsDefined(category);

    private static bool TryGetSlot(Entity<HumanoidAppearanceComponent> target, MarkingCategories category,
        int slot, out Marking marking)
    {
        marking = default!;

        if (!target.Comp.MarkingSet.TryGetCategory(category, out var markings))
            return false;

        if (slot < 0 || slot >= markings.Count)
            return false;

        marking = markings[slot];
        return true;
    }

    private void OnChangeMarking(MorphismChangeMarkingEvent args, EntitySessionEventArgs sessionArgs)
    {
        if (!IsAuthorized(args.TargetUser, sessionArgs, out var target))
            return;

        if (!IsKnownCategory(args.Category) || !TryGetSlot(target, args.Category, args.Slot, out _))
            return;

        if (!_prototype.TryIndex<MarkingPrototype>(args.MarkingId, out var prototype)
            || prototype.MarkingCategory != args.Category
            || !_markings.CanBeApplied(target.Comp.Species, target.Comp.Sex, prototype, _prototype))
            return;

        _humanoid.SetMarkingId(target, args.Category, args.Slot, args.MarkingId);
    }

    private void OnChangeMarkingColor(MorphismChangeMarkingColorEvent args, EntitySessionEventArgs sessionArgs)
    {
        if (!IsAuthorized(args.TargetUser, sessionArgs, out var target))
            return;

        if (!IsKnownCategory(args.Category) || !TryGetSlot(target, args.Category, args.Slot, out var marking))
            return;

        if (!_markings.TryGetMarking(marking, out var prototype) || args.Colors.Count != prototype.Sprites.Count)
            return;

        _humanoid.SetMarkingColor(target, args.Category, args.Slot, args.Colors);
    }

    private void OnSlotChange(MorphismSlotEvent args, EntitySessionEventArgs sessionArgs)
    {
        if (!IsAuthorized(args.TargetUser, sessionArgs, out var target))
            return;

        if (!IsKnownCategory(args.Category))
            return;

        if (args.Action == MorphismSlotEvent.SlotAction.Remove)
        {
            if (!TryGetSlot(target, args.Category, args.SlotIndex, out _))
                return;

            _humanoid.RemoveMarking(target, args.Category, args.SlotIndex);
        }
        else
        {
            var marking = _markings.MarkingsByCategoryAndSpecies(args.Category, target.Comp.Species)
                .Keys.FirstOrDefault();

            if (string.IsNullOrEmpty(marking))
                return;

            _humanoid.AddMarking(target, marking, Color.Black);
        }

        SendState(target, target);
    }

    private void OnBodyColorChange(MorphismChangeBodyColorEvent args, EntitySessionEventArgs sessionArgs)
    {
        if (!IsAuthorized(args.TargetUser, sessionArgs, out var target))
            return;

        var humanoid = target.Comp;

        if (args.Part == MorphismChangeBodyColorEvent.BodyPart.Skin)
        {
            humanoid.SkinColor = args.NewColor;
            Dirty(target.Owner, humanoid);

            var allBodyParts = Enum.GetValues<MarkingCategories>()
                .Where(c => c != MarkingCategories.Hair &&
                            c != MarkingCategories.FacialHair &&
                            c != MarkingCategories.Overlay &&
                            c != MarkingCategories.Undershirt &&
                            c != MarkingCategories.Underwear);

            foreach (var cat in allBodyParts)
            {
                if (humanoid.MarkingSet.TryGetCategory(cat, out var markings))
                {
                    for (int i = 0; i < markings.Count; i++)
                    {
                        var newColors = new List<Color>();
                        for (int j = 0; j < markings[i].MarkingColors.Count; j++)
                        {
                            newColors.Add(args.NewColor);
                        }
                        _humanoid.SetMarkingColor(target, cat, i, newColors);
                    }
                }
            }
        }
        else
        {
            humanoid.EyeColor = args.NewColor;
            _humanoid.SetBaseLayerColor(target, HumanoidVisualLayers.Eyes, args.NewColor);
            Dirty(target.Owner, humanoid);
        }
    }
}
