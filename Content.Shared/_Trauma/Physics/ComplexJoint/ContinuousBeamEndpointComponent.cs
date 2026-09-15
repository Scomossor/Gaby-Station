// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Trauma.Shared.Physics.ComplexJoint;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ContinuousBeamEndpointComponent : Component
{
    [DataField]
    public bool PvsOverride = true;

    [DataField, AutoNetworkedField]
    public EntityUid? Gun;
}
