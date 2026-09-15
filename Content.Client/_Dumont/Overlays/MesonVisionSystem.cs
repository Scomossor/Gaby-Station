// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Overlays;
using Content.Shared._Dumont.Overlays;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client._Dumont.Overlays;

/// <summary>
/// liga o overlay do meson enquanto o óculos estiver vestido e aceso
/// </summary>
public sealed class MesonVisionSystem : EquipmentHudSystem<MesonVisionComponent>
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;

    protected override SlotFlags TargetSlots => SlotFlags.EYES | SlotFlags.HEAD | SlotFlags.MASK;


    private float _range = 12f;
    private MesonVisionOverlay _edge = default!;

    public override void Initialize()
    {
        base.Initialize();

        _edge = new MesonVisionOverlay();

        // o refresh da base só roda em equipar/desequipar, então sem isso o
        // botão de ligar/desligar não chega aqui e o óculos fica preso ligado
        SubscribeLocalEvent<MesonVisionComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnHandleState(Entity<MesonVisionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshOverlay();
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<MesonVisionComponent> args)
    {
        base.UpdateInternal(args);

        var on = false;

        foreach (var comp in args.Components)
        {
            if (!comp.Enabled)
                continue;

            on = true;
            _range = comp.Range;
            break;
        }

        if (!on)
        {
            _overlay.RemoveOverlay(_edge);
            return;
        }

        _edge.Range = _range;
        _overlay.AddOverlay(_edge);
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _overlay.RemoveOverlay(_edge);
    }
}
