// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Physics;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Trauma.Shared.Physics.ComplexJoint;

[Serializable, NetSerializable]
public sealed class ContinuousBeamPositionEvent(NetCoordinates coords, bool shouldFire) : EntityEventArgs
{
    public NetCoordinates Coordinates = coords;

    public bool ShouldFire = shouldFire;
}

[ByRefEvent]
public record struct BeforeContinuousBeamDamagedEvent(EntityUid Uid, EntityUid Target, bool Cancelled = false);

[ByRefEvent]
public readonly record struct AfterContinuousBeamDamagedEvent(EntityUid Uid, EntityUid Target);

[ByRefEvent]
public readonly record struct ContinuousBeamStoppedFiringEvent;

[ByRefEvent]
public record struct BeforeContinuousBeamDamageTickEvent(
    Entity<ContinuousBeamGunComponent, ComplexJointVisualsComponent> Ent,
    bool Cancelled = false);
