// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Shared._Dumont.Overlays;

/// <summary>
/// o refresh de hud só chega na roupa vestida se o relay do tipo estiver
/// registrado à mão. o upstream registra os dele numa lista fechada, e o
/// RelayEvent é público, então dá pra registrar o nosso aqui sem editar lá
/// </summary>
public sealed class DumontHudRelaySystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, RefreshEquipmentHudEvent<ScannerVisionComponent>>(OnRelay);
        SubscribeLocalEvent<InventoryComponent, RefreshEquipmentHudEvent<MesonVisionComponent>>(OnMesonRelay);
    }

    private void OnRelay(Entity<InventoryComponent> ent, ref RefreshEquipmentHudEvent<ScannerVisionComponent> args)
    {
        _inventory.RelayEvent(ent, ref args);
    }

    private void OnMesonRelay(Entity<InventoryComponent> ent, ref RefreshEquipmentHudEvent<MesonVisionComponent> args)
    {
        _inventory.RelayEvent(ent, ref args);
    }
}
