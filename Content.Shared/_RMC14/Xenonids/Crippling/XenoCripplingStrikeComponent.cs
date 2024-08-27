﻿using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Crippling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoCripplingStrikeSystem))]
public sealed partial class XenoCripplingStrikeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float DamageMult = 1.2f;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = 20;

    [DataField, AutoNetworkedField]
    public TimeSpan ActiveDuration = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public FixedPoint2 SpeedMultiplier = FixedPoint2.New(0.75);

    [DataField, AutoNetworkedField]
    public TimeSpan SlowDuration = TimeSpan.FromSeconds(5);
}