// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Server.EntityEffects;

/// <summary>
/// Dumont - markings an <c>AddMarking</c> effect put on this humanoid, which is what
/// <c>RemoveMarking</c> is allowed to take off.
/// </summary>
[RegisterComponent, Access(typeof(AddMarkingEffectSystem), typeof(RemoveMarkingEffectSystem))]
public sealed partial class AddedMarkingsComponent : Component
{
    [ViewVariables]
    public HashSet<ProtoId<MarkingPrototype>> Markings = new();
}
