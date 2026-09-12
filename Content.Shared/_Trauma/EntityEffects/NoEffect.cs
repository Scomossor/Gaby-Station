// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityEffects;

public sealed partial class NoEffect : EventEntityEffect<NoEffect>
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-nothing", ("chance", Probability));
}

public sealed partial class NoEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExecuteEntityEffectEvent<NoEffect>>(OnNothing);
    }

    private void OnNothing(ref ExecuteEntityEffectEvent<NoEffect> args)
    {
    }
}
