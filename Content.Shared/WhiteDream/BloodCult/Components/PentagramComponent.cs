// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.WhiteDream.BloodCult.Components;

[NetworkedComponent, RegisterComponent]
public sealed partial class PentagramComponent : Component
{
    public ResPath RsiPath = new("/Textures/WhiteDream/BloodCult/Effects/pentagram.rsi");

    [DataField]
    public Vector2 Offset = new(0f, 0.6f);

    public readonly string[] States =
    [
        "halo1",
        "halo2",
        "halo3",
        "halo4",
        "halo5",
        "halo6"
    ];
}
