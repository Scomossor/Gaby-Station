using Content.Shared.Actions;
using Content.Shared._Dumont.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Item;
using Robust.Shared.Audio.Systems;
using Content.Shared.Item.ItemToggle;

namespace Content.Shared._Dumont.Clothing.Systems;

public sealed partial class EnvirohelmetToggleSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = null!;
    [Dependency] private SharedItemSystem _item = null!;
    [Dependency] private SharedAudioSystem _audio = null!;
    [Dependency] private SharedAppearanceSystem _appearance = null!;
    [Dependency] private ClothingSystem _clothing = null!;
    [Dependency] private ComponentTogglerSystem _componentTogglerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnvirohelmetToggleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EnvirohelmetToggleComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<EnvirohelmetToggleComponent, GetItemActionsEvent>(OnGetActions);
        SubscribeLocalEvent<EnvirohelmetToggleComponent, ToggleEnvirohelmetEvent>(OnToggle);
    }

    private void OnMapInit(Entity<EnvirohelmetToggleComponent> ent, ref MapInitEvent args)
    {
        var (uid, comp) = ent;

        _actions.AddAction(uid, ref comp.ActionEntity, comp.Action);
        _actions.SetToggled(comp.ActionEntity, comp.IsActive);
        UpdateState(ent, comp.IsActive);
        Dirty(uid, comp);
    }

    private void OnShutdown(Entity<EnvirohelmetToggleComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.ActionEntity != null)
            _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    private void OnGetActions(Entity<EnvirohelmetToggleComponent> ent, ref GetItemActionsEvent args)
    {
        if (ent.Comp.ActionEntity != null)
            args.AddAction(ent.Comp.ActionEntity.Value);
    }

    private void OnToggle(Entity<EnvirohelmetToggleComponent> ent, ref ToggleEnvirohelmetEvent args)
    {
        var (uid, comp) = ent;
        var nextActive = !comp.IsActive;
        _audio.PlayPredicted(ent.Comp.ToggleSound, ent, args.Performer);
        UpdateState(ent, nextActive);
        _actions.SetToggled(comp.ActionEntity, nextActive);
        args.Handled = true;
    }

    private void UpdateState(Entity<EnvirohelmetToggleComponent> ent, bool active)
    {
        var (uid, comp) = ent;
        comp.IsActive = active;
        Dirty(uid, comp);

        _appearance.SetData(uid, EnvirohelmetVisuals.IsOpen, active);

        if (active)
        {
            var target = comp.Parent ? Transform(uid).ParentUid : uid;

            if (TerminatingOrDeleted(target))
                return;

            comp.Target = target;
            EntityManager.AddComponents(target, comp.Components);
        }
        else
        {
            if (comp.Target == null)
                return;

            if (TerminatingOrDeleted(comp.Target.Value))
                return;

            EntityManager.RemoveComponents(comp.Target.Value, comp.RemoveComponents ?? comp.Components);
            if (comp.ClosedComponents.Count > 0)
                EntityManager.AddComponents(comp.Target.Value, comp.ClosedComponents);
            comp.Target = null;
        }
    }
}
