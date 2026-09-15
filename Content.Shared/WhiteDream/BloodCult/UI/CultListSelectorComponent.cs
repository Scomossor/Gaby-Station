// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._White.ListViewSelector;
using Robust.Shared.GameStates;

namespace Content.Shared.WhiteDream.BloodCult.UI;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CultListSelectorComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ListViewSelectorEntry> Entries = new();
}
