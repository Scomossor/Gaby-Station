using System.Linq;
using Content.Client.Clothing.Components;
using Content.Shared._Dumont.Clothing.Components;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Light.Components;
using Content.Shared.Toggleable;
using Content.Client.Items.Systems;
using Content.Client.Toggleable;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Clothing.EntitySystems;

public sealed class EnvirohelmetVisualizerSystem : VisualizerSystem<EnvirohelmetVisualsComponent>
{
    [Dependency] private readonly SharedItemSystem _itemSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EnvirohelmetVisualsComponent, GetEquipmentVisualsEvent>(OnGetEquipmentVisuals,
            after: new[] { typeof(ClientClothingSystem), typeof(ToggleableVisualsSystem) });
        SubscribeLocalEvent<EnvirohelmetVisualsComponent, GetInhandVisualsEvent>(OnGetHeldVisuals,
            after: new[] { typeof(ItemSystem) });
    }

    protected override void OnAppearanceChange(EntityUid uid, EnvirohelmetVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (!AppearanceSystem.TryGetData<bool>(uid, EnvirohelmetVisuals.IsOpen, out var isOpen, args.Component))
            return;

        var lightEnabled = AppearanceSystem.TryGetData<bool>(uid, ToggleableVisuals.Enabled, out var enabled, args.Component) && enabled;
        var modulateColor = AppearanceSystem.TryGetData<Color>(uid, ToggleableVisuals.Color, out var color, args.Component);

        if (args.Sprite != null)
        {
            if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), "closed", out var closedLayer, false))
                SpriteSystem.LayerSetVisible((uid, args.Sprite), closedLayer, !isOpen);

            if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), component.SpriteLayer, out var openLayer, false))
                SpriteSystem.LayerSetVisible((uid, args.Sprite), openLayer, isOpen);

            if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), "light", out var closedLightLayer, false))
                SpriteSystem.LayerSetVisible((uid, args.Sprite), closedLightLayer, !isOpen && lightEnabled);

            if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), "open-light", out var openLightLayer, false))
                SpriteSystem.LayerSetVisible((uid, args.Sprite), openLightLayer, isOpen && lightEnabled);

            if (modulateColor)
            {
                if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), "light", out var lightColorLayer, false))
                    SpriteSystem.LayerSetColor((uid, args.Sprite), lightColorLayer, color);

                if (SpriteSystem.LayerMapTryGet((uid, args.Sprite), "open-light", out var openLightColorLayer, false))
                    SpriteSystem.LayerSetColor((uid, args.Sprite), openLightColorLayer, color);
            }
        }

        if (TryComp<ItemTogglePointLightComponent>(uid, out var toggleLights) &&
            TryComp(uid, out PointLightComponent? light))
        {
            DebugTools.Assert(!light.NetSyncEnabled,
                $"{typeof(ItemTogglePointLightComponent)} requires point lights without net-sync");
            _pointLight.SetEnabled(uid, lightEnabled, light);
            if (modulateColor && toggleLights.ToggleableVisualsColorModulatesLights)
                _pointLight.SetColor(uid, color, light);
        }

        _itemSystem.VisualsChanged(uid);
    }

    private void OnGetEquipmentVisuals(Entity<EnvirohelmetVisualsComponent> envirohelmet, ref GetEquipmentVisualsEvent args)
    {
        var (uid, comp) = envirohelmet;

        if (!TryComp(uid, out AppearanceComponent? appearance)
            || !AppearanceSystem.TryGetData<bool>(uid, EnvirohelmetVisuals.IsOpen, out var isOpen, appearance)
            || !isOpen)
            return;

        if (!comp.ClothingVisuals.TryGetValue(args.Slot, out var layers))
            return;

        args.Layers.RemoveAll(layer => layer.Item2.State?.StartsWith("open-") != true);

        var lightEnabled = AppearanceSystem.TryGetData<bool>(uid, ToggleableVisuals.Enabled, out var enabled, appearance) && enabled;

        foreach (var layer in layers)
        {
            var state = layer.State ?? string.Empty;
            if (!state.StartsWith("open-"))
                continue;

            if (state.EndsWith("-light") && !lightEnabled)
                continue;

            var key = layer.MapKeys?.FirstOrDefault() ??
                      (state.EndsWith("-light") ? $"{args.Slot}-open-light" : $"{args.Slot}-open");
            args.Layers.Add((key, layer));
        }
    }

    private void OnGetHeldVisuals(EntityUid uid, EnvirohelmetVisualsComponent component, GetInhandVisualsEvent args)
    {
        if (!TryComp(uid, out AppearanceComponent? appearance)
            || !AppearanceSystem.TryGetData<bool>(uid, EnvirohelmetVisuals.IsOpen, out var isOpen, appearance)
            || !isOpen)
            return;

        if (!component.InhandVisuals.TryGetValue(args.Location, out var layers))
            return;

        args.Layers.RemoveAll(layer => layer.Item2.State?.StartsWith("open-") != true);

        var lightEnabled = AppearanceSystem.TryGetData<bool>(uid, ToggleableVisuals.Enabled, out var enabled, appearance) && enabled;

        foreach (var layer in layers)
        {
            var state = layer.State ?? string.Empty;
            if (!state.StartsWith("open-"))
                continue;

            if (state.EndsWith("-light"))
                continue;

            var key = layer.MapKeys?.FirstOrDefault() ?? $"open-{args.Location.ToString().ToLowerInvariant()}";
            args.Layers.Add((key, layer));
        }
    }
}
