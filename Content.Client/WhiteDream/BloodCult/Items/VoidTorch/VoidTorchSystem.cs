// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Light.Components;
using Content.Client.Light.EntitySystems;
using Content.Shared.WhiteDream.BloodCult;
using Content.Shared.WhiteDream.BloodCult.Items.VoidTorch;
using Robust.Client.GameObjects;

namespace Content.Client.WhiteDream.BloodCult.Items.VoidTorch;

public sealed partial class VoidTorchSystem : VisualizerSystem<VoidTorchComponent>
{
    [Dependency] private LightBehaviorSystem _lightBehavior = default!;

    protected override void OnAppearanceChange(EntityUid uid,
        VoidTorchComponent component,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;
        if (!AppearanceSystem.TryGetData<bool>(uid, GenericCultVisuals.State, out var state)
            || !TryComp<LightBehaviourComponent>(uid, out var lightBehaviour))
            return;

        _lightBehavior.StopLightBehaviour((uid, lightBehaviour), resetToOriginalSettings: true);
        _lightBehavior.StartLightBehaviour((uid, lightBehaviour),
            state ? component.TurnOnLightBehaviour : component.TurnOffLightBehaviour);
    }
}
