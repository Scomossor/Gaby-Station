// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.Weapons.Ranged.Systems;
using Content.Trauma.Shared.Weapons.Ranged.Components;
using Content.Trauma.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Trauma.Client.Weapons.Ranged;

public sealed partial class MultiMagazineGunSystem : SharedMultiMagazineGunSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MultiMagazineAmmoProviderComponent, GunSystem.UpdateAmmoCounterEvent>(OnMagazineAmmoUpdate);
        SubscribeLocalEvent<MultiMagazineAmmoProviderComponent, GunSystem.AmmoCounterControlEvent>(OnMagazineControl);
    }

    private void OnMagazineAmmoUpdate(Entity<MultiMagazineAmmoProviderComponent> ent,
        ref GunSystem.UpdateAmmoCounterEvent args)
    {
        if (args.Control is not BoxContainer container)
            return;

        var controls = container.Children.ToList();
        var index = 0;
        foreach (var (slot, magEnt) in GetMagazineEntities(ent))
        {
            if (index >= controls.Count)
                break;

            var control = controls[index++];
            if (magEnt is not { } uid)
            {
                control.Visible = false;
                continue;
            }

            control.Visible = true;

            if (ent.Comp.Slots[slot] is { } multiplier)
            {
                var ev = new GunSystem.UpdateAmmoCounterEvent
                {
                    FireCostMultiplier = multiplier,
                    Control = control,
                };

                RaiseLocalEvent(uid, ev);
                continue;
            }

            var childEv = new GunSystem.UpdateAmmoCounterEvent
            {
                FireCostMultiplier = args.FireCostMultiplier,
                Control = control,
            };
            RaiseLocalEvent(uid, childEv);
        }
    }

    private void OnMagazineControl(Entity<MultiMagazineAmmoProviderComponent> ent,
        ref GunSystem.AmmoCounterControlEvent args)
    {
        foreach (var (slot, magEnt) in GetMagazineEntities(ent))
        {
            Control control = ent.Comp.Slots[slot] is null
                ? new GunSystem.DefaultStatusControl()
                : new GunSystem.BoxesStatusControl();

            control.Visible = magEnt != null;
            args.Controls.Add(control);
        }
    }

    protected override void OnMagazineSlotChange(EntityUid uid,
        MultiMagazineAmmoProviderComponent component,
        ContainerModifiedMessage args)
    {
        if (!component.Slots.ContainsKey(args.Container.ID))
            return;

        base.OnMagazineSlotChange(uid, component, args);
    }
}