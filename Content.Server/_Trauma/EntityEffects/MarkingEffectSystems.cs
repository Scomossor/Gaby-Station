// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Humanoid;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Trauma.Shared.EntityEffects;

namespace Content.Trauma.Server.EntityEffects;

public sealed partial class AddMarkingEffectSystem : TraumaEntityEffectSystem<HumanoidAppearanceComponent, AddMarking>
{
    [Dependency] private MarkingManager _markingManager = default!;
    [Dependency] private HumanoidAppearanceSystem _humanoid = default!;

    protected override void Effect(Entity<HumanoidAppearanceComponent> ent, AddMarking effect, EntityEffectBaseArgs args)
    {
        if (!_markingManager.Markings.TryGetValue(effect.Marking, out var proto))
            return;

        if (ent.Comp.MarkingSet.TryGetMarking(proto.MarkingCategory, effect.Marking, out _))
            return;

        var colors = MarkingColoring.GetMarkingLayerColors(proto, ent.Comp.SkinColor, ent.Comp.EyeColor, ent.Comp.MarkingSet);
        _humanoid.SetLayerVisibility(ent.AsNullable(), proto.BodyPart, true);
        _humanoid.AddMarking(ent, effect.Marking, colors, sync: true, forced: true, ent.Comp);
        EnsureComp<AddedMarkingsComponent>(ent).Markings.Add(effect.Marking);
    }
}

public sealed partial class RemoveMarkingEffectSystem : TraumaEntityEffectSystem<HumanoidAppearanceComponent, RemoveMarking>
{
    [Dependency] private HumanoidAppearanceSystem _humanoid = default!;

    protected override void Effect(Entity<HumanoidAppearanceComponent> ent, RemoveMarking effect, EntityEffectBaseArgs args)
    {
        if (!TryComp<AddedMarkingsComponent>(ent, out var added) || !added.Markings.Remove(effect.Marking))
            return;

        _humanoid.RemoveMarking(ent, effect.Marking, sync: true, ent.Comp);
        if (added.Markings.Count == 0)
            RemComp(ent, added);
    }
}
