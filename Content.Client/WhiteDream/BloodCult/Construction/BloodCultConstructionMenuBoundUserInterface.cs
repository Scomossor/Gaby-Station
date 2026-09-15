// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.Construction;
using Content.Client.UserInterface.Controls;
using Content.Shared.Construction.Prototypes;
using Content.Shared.WhiteDream.BloodCult.Construction;
using Content.Shared._White.RadialSelector;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.WhiteDream.BloodCult.Construction;

[UsedImplicitly]
public sealed partial class BloodCultConstructionMenuBoundUserInterface : BoundUserInterface
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;

    private readonly ConstructionSystem _constructionSystem;
    private SimpleRadialMenu? _menu;

    public BloodCultConstructionMenuBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _constructionSystem = EntMan.System<ConstructionSystem>();
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<BloodCultConstructionComponent>(Owner, out var construction))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);
        _menu.SetButtons(ConvertToButtons(construction.Entries));
        _menu.OpenCentered();
    }

    private IEnumerable<RadialMenuOption> ConvertToButtons(IReadOnlyList<RadialSelectorEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (entry.Category != null)
            {
                yield return new RadialMenuNestedLayerOption(ConvertToButtons(entry.Category.Entries).ToList())
                {
                    Sprite = entry.Category.Icon, // Dumont
                    ToolTip = entry.Category.Name,
                };
                continue;
            }

            if (entry.Prototype == null)
                continue;

            yield return new RadialMenuActionOption<string>(HandleMenuOptionClick, entry.Prototype)
            {
                Sprite = GetSprite(entry), // Dumont
                ToolTip = GetTooltip(entry),
            };
        }
    }

    private void HandleMenuOptionClick(string prototypeId)
    {
        SendPredictedMessage(new BloodCultConstructionSelectedMessage(prototypeId));
    }

    private string GetTooltip(RadialSelectorEntry entry)
    {
        if (!string.IsNullOrEmpty(entry.Name))
            return entry.Name;

        if (entry.Prototype != null && _prototypeManager.TryIndex(entry.Prototype, out ConstructionPrototype? constructionPrototype))
            return constructionPrototype.Name ?? entry.Prototype;

        return entry.Prototype ?? string.Empty;
    }

    private SpriteSpecifier? GetSprite(RadialSelectorEntry entry)
    {
        if (entry.Icon != null)
            return entry.Icon;

        if (entry.Prototype == null)
            return null;

        if (_prototypeManager.TryIndex(entry.Prototype, out EntityPrototype? entityPrototype))
            return GetEntityPrototypeIcon(entityPrototype);

        if (!_prototypeManager.TryIndex(entry.Prototype, out ConstructionPrototype? constructionPrototype))
            return null;

        if (!_constructionSystem.TryGetRecipePrototype(constructionPrototype.ID, out var targetProtoId)
            || !_prototypeManager.TryIndex(targetProtoId, out EntityPrototype? targetPrototype))
            return null;

        return GetEntityPrototypeIcon(targetPrototype);
    }

    private SpriteSpecifier? GetEntityPrototypeIcon(EntityPrototype prototype)
    {
        if (prototype.TryGetComponent(out IconComponent? icon, _componentFactory))
            return icon.Icon;

        if (prototype.Components.ContainsKey("Sprite"))
            return new SpriteSpecifier.EntityPrototype(prototype.ID);

        return null;
    }
}
