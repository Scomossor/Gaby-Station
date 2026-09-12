// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.StatusEffects;

[RegisterComponent, NetworkedComponent]
public sealed partial class StatusEffectEffectsApplyComponent : Component
{
    [DataField]
    public EntityEffect[]? EffectsOnApply;

    [DataField]
    public EntityEffect[]? EffectsOnRemoval;
}
