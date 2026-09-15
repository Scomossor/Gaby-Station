// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Server.WhiteDream.BloodCult.Components;

[RegisterComponent]
public sealed partial class TwistedConstructionTargetComponent : Component
{
    [DataField(required: true)]
    public EntProtoId ReplacementProto = "";

    [DataField]
    public TimeSpan DoAfterDelay = TimeSpan.FromSeconds(2);
}
