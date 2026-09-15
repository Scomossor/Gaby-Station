// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Shared._White.ListViewSelector;
using Content.Shared.WhiteDream.BloodCult.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.WhiteDream.BloodCult.UI;

[UsedImplicitly]
public sealed class CultListSelectorBUI(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private FancyWindow? _window;
    private BoxContainer? _itemsContainer;

    protected override void Open()
    {
        base.Open();

        _window = FormWindow();
        _window.OnClose += Close;

        Refresh();

        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        Refresh();
    }

    public override void Update()
    {
        base.Update();
        Refresh();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Close();
    }

    private FancyWindow FormWindow()
    {
        var window = new FancyWindow
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            MinWidth = 350,
            MinHeight = 400,
            Title = Loc.GetString("cult-list-selector-title")
        };

        var scrollContainer = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true
        };

        var itemsContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical
        };

        scrollContainer.AddChild(itemsContainer);
        window.AddChild(scrollContainer);

        _itemsContainer = itemsContainer;

        return window;
    }

    private void Refresh()
    {
        if (_itemsContainer is null ||
            !EntMan.TryGetComponent(Owner, out CultListSelectorComponent? selector))
            return;

        if (_itemsContainer.ChildCount == selector.Entries.Count)
            return;

        _itemsContainer.RemoveAllChildren();

        foreach (var entry in selector.Entries)
        {
            var button = new Button { Text = entry.Name };
            var selected = entry;

            button.OnButtonUp += _ =>
            {
                SendMessage(new ListViewItemSelectedMessage(selected, selector.Entries.IndexOf(selected), new()));
                Close();
            };

            _itemsContainer.AddChild(button);
        }
    }
}
