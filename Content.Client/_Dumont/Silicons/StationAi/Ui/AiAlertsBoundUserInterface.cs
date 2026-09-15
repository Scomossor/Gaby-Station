// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Dumont.Silicons.StationAi;
using Robust.Client.UserInterface;

namespace Content.Client._Dumont.Silicons.StationAi.Ui;

public sealed class AiAlertsBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private AiAlertsMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<AiAlertsMenu>();
        _menu.OnWarpTo += target => SendMessage(new AiAlertWarpMessage(target));
        _menu.OnDismiss += target => SendMessage(new AiAlertDismissMessage(target));

        SendMessage(new AiAlertsRefreshMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is AiAlertsBuiState cast)
            _menu?.Update(cast);
    }
}
