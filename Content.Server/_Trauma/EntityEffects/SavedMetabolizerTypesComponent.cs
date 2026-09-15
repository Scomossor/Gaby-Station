// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Body.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Server.EntityEffects;

/// <summary>
/// Dumont - metabolizer types saved by <c>SaveMetabolizerTypes</c>, by key, until restored.
/// </summary>
[RegisterComponent, Access(typeof(SaveMetabolizerTypesEffectSystem), typeof(RestoreMetabolizerTypesEffectSystem))]
public sealed partial class SavedMetabolizerTypesComponent : Component
{
    [ViewVariables]
    public Dictionary<string, HashSet<ProtoId<MetabolizerTypePrototype>>?> Saved = new();
}
