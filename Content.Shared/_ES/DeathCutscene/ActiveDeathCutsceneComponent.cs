// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._ES.DeathCutscene;

[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveDeathCutsceneComponent : Component
{
    [DataField]
    public TimeSpan GhostTime;

    [DataField]
    public bool CanReturnToBody = true;
}
