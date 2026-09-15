// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.WhiteDream.BloodCult.Repulsor;

[RegisterComponent]
public sealed partial class RepulseComponent : Component
{
    [DataField]
    public float Distance = 2f;

    [DataField]
    public float Speed = 5f;

    [DataField]
    public TimeSpan KnockdownDuration = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(1);
}

public sealed class BeforeRepulseEvent(EntityUid target) : CancellableEntityEventArgs
{
    public EntityUid Target = target;
}
