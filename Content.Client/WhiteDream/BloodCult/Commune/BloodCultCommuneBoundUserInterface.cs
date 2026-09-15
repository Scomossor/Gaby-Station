// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.WhiteDream.BloodCult.Commune;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.WhiteDream.BloodCult.Commune;

[UsedImplicitly]
public sealed partial class BloodCultCommuneBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private BloodCultCommuneWindow? _window;

    public BloodCultCommuneBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<BloodCultCommuneWindow>();
        _window.OnCommune += OnCommuneSent;
    }

    private void OnCommuneSent(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        SendMessage(new BloodCultCommuneSendMessage(message));
        _window?.Close();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is BloodCultCommuneBuiState cast)
            _window?.UpdateState(cast.Message);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Close();
    }
}
