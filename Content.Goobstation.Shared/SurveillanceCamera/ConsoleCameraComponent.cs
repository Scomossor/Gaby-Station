// SPDX-FileCopyrightText: 2026 Project Dumont
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.SurveillanceCamera;

/// <summary>
/// Adds opt-in support for systems that need to use normal surveillance cameras
/// as restricted remote console viewpoints.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ConsoleCameraComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<string> Tags = [];

    [DataField, AutoNetworkedField]
    public float Range = 7.5f;
}
