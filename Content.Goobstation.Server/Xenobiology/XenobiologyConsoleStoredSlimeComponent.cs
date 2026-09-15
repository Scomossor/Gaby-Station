// SPDX-FileCopyrightText: 2026 Project Dumont
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameObjects;

namespace Content.Goobstation.Server.Xenobiology;

[RegisterComponent]
public sealed partial class XenobiologyConsoleStoredSlimeComponent : Component
{
    [ViewVariables]
    public bool? HtnWasEnabled;

    [ViewVariables]
    public EntityUid Console;
}
