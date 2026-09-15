// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Projectiles;

/// <summary>
/// Raised directed on an entity when it stops being embedded in another entity.
/// </summary>
/// <param name="Detacher">Who pulled it out, if anyone did.</param>
/// <param name="Embedded">Entity it was embedded in.</param>
[ByRefEvent]
public readonly record struct EmbedDetachEvent(EntityUid? Detacher, EntityUid Embedded)
{
    public readonly EntityUid? Detacher = Detacher;

    /// <summary>
    /// Entity that it was embedded in.
    /// </summary>
    public readonly EntityUid Embedded = Embedded;
}
