// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Shared.WhiteDream.BloodCult.Spells;

[Prototype]
public sealed partial class CultPowerPoolPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ViewVariables]
    [DataField]
    public List<string> Powers = new();
}
