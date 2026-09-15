// SPDX-FileCopyrightText: 2026 Project Dumont
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Server.Xenobiology;

[RegisterComponent]
public sealed partial class XenobiologyConsoleRemoteComponent : Component
{
    [ViewVariables]
    public EntityUid? Controller;

    [ViewVariables]
    public bool ReturningToCameraView;
}
