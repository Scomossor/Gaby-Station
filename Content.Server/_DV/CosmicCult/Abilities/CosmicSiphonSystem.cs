// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Ghost;
using Content.Server.Light.Components;
using Content.Shared._DV.CosmicCult;
using Content.Shared._DV.CosmicCult.Abilities;
using Content.Shared._DV.CosmicCult.Components;
using Robust.Shared.Random;

namespace Content.Server._DV.CosmicCult.Abilities;

public sealed partial class CosmicSiphonSystem : SharedCosmicSiphonSystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private GhostSystem _ghost = default!;
    [Dependency] private IRobustRandom _random = default!;

    private readonly HashSet<Entity<PoweredLightComponent>> _lights = [];

    protected override void OnSiphonDoAfter(Entity<CosmicCultComponent> ent, ref CosmicSiphonDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        base.OnSiphonDoAfter(ent, ref args);

        if (!ent.Comp.CosmicEmpowered)
            return;

        // if you're empowered there's a 20% chance to flicker each light on siphon. Not predicted because GhostSystem isn't (and who cares anyway).
        _lights.Clear();
        _lookup.GetEntitiesInRange(Transform(ent).Coordinates, ent.Comp.FlickerRange, _lights, LookupFlags.StaticSundries);
        foreach (var light in _lights)
        {
            if (!_random.Prob(ent.Comp.FlickerProbability))
                continue;

            _ghost.DoGhostBooEvent(light);
        }
    }
}
