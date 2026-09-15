// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._DV.CosmicCult.Components;
using Robust.Client.UserInterface;

namespace Content.Client._DV.CosmicCult.UI.CosmicShop;

public sealed partial class CosmicShopBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables] private CosmicShopMenu? _menu;

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<CosmicShopMenu>();

        _menu.OnGainButtonPressed += id => SendPredictedMessage(new InfluenceSelectedMessage(id));
        _menu.OnLevelUpConfirmed += () => SendPredictedMessage(new LevelUpconfirmedMessage());
        _menu.OnRespecConfirmed += () => SendPredictedMessage(new RespecConfirmedMessage());
    }
}
