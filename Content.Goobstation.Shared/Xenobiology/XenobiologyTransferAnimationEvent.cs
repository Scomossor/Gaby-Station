// SPDX-FileCopyrightText: 2026 Project Dumont
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.Xenobiology;

[Serializable, NetSerializable]
public sealed class XenobiologyTransferAnimationEvent : EntityEventArgs
{
    public readonly NetCoordinates Coordinates;
    public readonly List<NetEntity> Targets;
    public readonly XenobiologyTransferAnimationType Type;

    public XenobiologyTransferAnimationEvent(
        NetCoordinates coordinates,
        List<NetEntity> targets,
        XenobiologyTransferAnimationType type)
    {
        Coordinates = coordinates;
        Targets = targets;
        Type = type;
    }
}

[Serializable, NetSerializable]
public enum XenobiologyTransferAnimationType : byte
{
    Suction,
    Release,
}
