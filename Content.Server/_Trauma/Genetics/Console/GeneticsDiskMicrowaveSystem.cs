// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Kitchen.Components;
using Content.Trauma.Shared.Genetics.Console;

namespace Content.Trauma.Server.Genetics.Console;

public sealed partial class GeneticsDiskMicrowaveSystem : EntitySystem
{
    [Dependency] private GeneticsDiskSystem _disk = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GeneticsDiskComponent, BeingMicrowavedEvent>(OnMicrowaved);
    }

    private void OnMicrowaved(EntityUid uid, GeneticsDiskComponent component, BeingMicrowavedEvent args)
    {
        _disk.Wipe((uid, component));
    }
}
