// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Body.Components;
using Content.Shared.EntityEffects;
using Content.Trauma.Shared.EntityEffects;

namespace Content.Trauma.Server.EntityEffects;

public sealed partial class AddMetabolizerTypeEffectSystem : TraumaEntityEffectSystem<MetabolizerComponent, AddMetabolizerType>
{
    protected override void Effect(Entity<MetabolizerComponent> ent, AddMetabolizerType effect, EntityEffectBaseArgs args)
    {
        ent.Comp.MetabolizerTypes ??= new();
        ent.Comp.MetabolizerTypes.Add(effect.Type);
    }
}

public sealed partial class RemoveMetabolizerTypeEffectSystem : TraumaEntityEffectSystem<MetabolizerComponent, RemoveMetabolizerType>
{
    protected override void Effect(Entity<MetabolizerComponent> ent, RemoveMetabolizerType effect, EntityEffectBaseArgs args)
    {
        ent.Comp.MetabolizerTypes?.Remove(effect.Type);
    }
}
