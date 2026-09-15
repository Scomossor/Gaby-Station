// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Trauma.Shared.Light;

/// <summary>
/// System for assigning random values to <see cref="SharedPointLightComponent"/> variables when given <see cref="RandomPointLightComponent"/>
/// </summary>
/// <remarks>
/// </remarks>
public sealed partial class RandomPointLightSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedPointLightSystem _light = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomPointLightComponent, MapInitEvent>(RandomLight);
    }

    private void RandomLight(Entity<RandomPointLightComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        var rpl = ent.Comp;

        // Keeping the V variable between 0.5 and 1.0 so that it's always bright
        var hsv = new Vector4(
            _random.NextFloat(0, 1),
            _random.NextFloat(0, 1),
            _random.NextFloat(0.5f, 1),
            1
        );

        var color = Color.FromHsv(hsv);

        _light.SetRadius(ent, _random.NextFloat(rpl.MinRadius, rpl.MaxRadius));
        _light.SetEnergy(ent, _random.NextFloat(rpl.MinEnergy, rpl.MaxEnergy));
        _light.SetColor(ent, color);
    }
}
