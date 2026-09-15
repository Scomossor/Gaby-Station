// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Overlays;
using Content.Shared._Dumont.Silicons.Borgs;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Dumont.Overlays;

/// <summary>
/// põe a faixa de carga em cima dos silicons pra quem está de HUD de diagnóstico.
/// </summary>
public sealed class ShowSiliconChargeIconsSystem : EquipmentHudSystem<ShowSiliconChargeIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconChargeStateComponent, GetStatusIconsEvent>(OnGetStatusIcons);
    }

    private void OnGetStatusIcons(Entity<SiliconChargeStateComponent> ent, ref GetStatusIconsEvent args)
    {
        if (!IsActive)
            return;

        if (ent.Comp.Level == SiliconChargeLevel.Full)
            return;

        if (_proto.TryIndex<SiliconChargeIconPrototype>(IconId(ent.Comp.Level), out var icon))
            args.StatusIcons.Add(icon);
    }

    private static string IconId(SiliconChargeLevel level)
    {
        return level switch
        {
            SiliconChargeLevel.None => "SiliconChargeNone",
            SiliconChargeLevel.Critical => "SiliconChargeCritical",
            SiliconChargeLevel.Low => "SiliconChargeLow",
            SiliconChargeLevel.Half => "SiliconChargeHalf",
            _ => "SiliconChargeHigh",
        };
    }
}
