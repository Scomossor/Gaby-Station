// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Shared.WhiteDream.BloodCult.Runes;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.WhiteDream.BloodCult.Runes.UI;

[UsedImplicitly]
public sealed partial class RuneDrawerBUI : BoundUserInterface
{
    private static readonly Color SectorColor = new(48, 12, 16, 205);
    private static readonly Color SectorHoverColor = new(122, 22, 30, 225);

    [Dependency] private IPrototypeManager _protoManager = default!;

    private SimpleRadialMenu? _menu;

    public RuneDrawerBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SimpleRadialMenu>();
        _menu.Track(Owner);

        if (State is RuneDrawerMenuState runeDrawerState)
            _menu.SetButtons(ConvertToButtons(runeDrawerState.AvailalbeRunes));

        _menu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_menu is not null && state is RuneDrawerMenuState runeDrawerState)
            _menu.SetButtons(ConvertToButtons(runeDrawerState.AvailalbeRunes));
    }

    private List<RadialMenuOption> ConvertToButtons(List<ProtoId<RuneSelectorPrototype>>? runes)
    {
        var models = new List<RadialMenuOption>();

        if (runes is null)
            return models;

        foreach (var runeSelector in runes)
        {
            if (!_protoManager.TryIndex(runeSelector, out var runeSelectorProto) ||
                !_protoManager.TryIndex(runeSelectorProto.Prototype, out var runeProto))
                continue;

            models.Add(new RadialMenuActionOption<ProtoId<RuneSelectorPrototype>>(OnRunePressed, runeSelector)
            {
                ToolTip = runeProto.Name,
                Sprite = new SpriteSpecifier.EntityPrototype(runeSelectorProto.Prototype),
                BackgroundColor = SectorColor,
                HoverBackgroundColor = SectorHoverColor
            });
        }

        return models;
    }

    private void OnRunePressed(ProtoId<RuneSelectorPrototype> runeSelector)
    {
        SendMessage(new RuneDrawerSelectedMessage(runeSelector));
        Close();
    }
}
