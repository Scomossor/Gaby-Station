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

public sealed partial class SaveMetabolizerTypesEffectSystem : TraumaEntityEffectSystem<MetabolizerComponent, SaveMetabolizerTypes>
{
    protected override void Effect(Entity<MetabolizerComponent> ent, SaveMetabolizerTypes effect, EntityEffectBaseArgs args)
    {
        var saved = EnsureComp<SavedMetabolizerTypesComponent>(ent);
        if (saved.Saved.ContainsKey(effect.Key))
            return;

        saved.Saved[effect.Key] = ent.Comp.MetabolizerTypes is {} types ? new(types) : null;
    }
}

public sealed partial class RestoreMetabolizerTypesEffectSystem : TraumaEntityEffectSystem<MetabolizerComponent, RestoreMetabolizerTypes>
{
    protected override void Effect(Entity<MetabolizerComponent> ent, RestoreMetabolizerTypes effect, EntityEffectBaseArgs args)
    {
        if (!TryComp<SavedMetabolizerTypesComponent>(ent, out var saved) ||
            !saved.Saved.Remove(effect.Key, out var types))
            return;

        ent.Comp.MetabolizerTypes = types is null ? null : new(types);
        if (saved.Saved.Count == 0)
            RemComp(ent, saved);
    }
}
