// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Overlays;
using Content.Shared._Dumont.Overlays;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;

namespace Content.Client._Dumont.Overlays;

/// <summary>
/// liga o overlay de estrutura enquanto o óculos estiver vestido, no mesmo
/// molde que a visão noturna e a térmica usam
/// </summary>
public sealed class ScannerVisionSystem : EquipmentHudSystem<ScannerVisionComponent>
{
    [Dependency] private readonly IOverlayManager _overlay = default!;

    private ScannerVisionOverlay _visionOverlay = default!;

    protected override SlotFlags TargetSlots => SlotFlags.EYES | SlotFlags.HEAD | SlotFlags.MASK;

    public override void Initialize()
    {
        base.Initialize();

        _visionOverlay = new ScannerVisionOverlay();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ScannerVisionComponent> args)
    {
        base.UpdateInternal(args);

        foreach (var comp in args.Components)
        {
            _visionOverlay.Comp = comp;
            break;
        }

        _overlay.AddOverlay(_visionOverlay);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _overlay.RemoveOverlay(_visionOverlay);
        _visionOverlay.Comp = null;
    }
}
