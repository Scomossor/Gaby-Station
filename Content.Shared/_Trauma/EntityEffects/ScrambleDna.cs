// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class ScrambleDna : EventEntityEffect<ScrambleDna>
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-scramble-dna", ("chance", Probability));
}

public sealed partial class ScrambleDnaEntityEffectSystem : TraumaEntityEffectSystem<HumanoidAppearanceComponent, ScrambleDna>
{
    [Dependency] private DnaScrambleOnTriggerSystem _scramble = default!;

    protected override void Effect(Entity<HumanoidAppearanceComponent> ent, ScrambleDna effect, EntityEffectBaseArgs args)
    {
        _scramble.Scramble(ent.Owner, ent.Comp);
    }
}
