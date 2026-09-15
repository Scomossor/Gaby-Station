// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.WhiteDream.BloodCult;
using Content.Shared.WhiteDream.BloodCult.Components;
using Robust.Client.GameObjects;

namespace Content.Client.WhiteDream.BloodCult;

public sealed partial class PylonVisualizerSystem : VisualizerSystem<PylonComponent>
{
    protected override void OnAppearanceChange(EntityUid uid,
        PylonComponent component,
        ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null ||
            !AppearanceSystem.TryGetData<bool>(uid,
                PylonVisuals.Activated,
                out var active,
                args.Component))
        {
            return;
        }

        // Dumont
        SpriteSystem.LayerSetAutoAnimated((uid, args.Sprite), PylonVisuals.BaseLayer, active);
        SpriteSystem.LayerSetAutoAnimated((uid, args.Sprite), PylonVisuals.Layer, active);
    }
}
