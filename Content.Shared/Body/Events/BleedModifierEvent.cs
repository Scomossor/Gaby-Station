// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared.Body.Events;

/// <summary>
/// Raised on an entity before they bleed, so other systems can change how much blood is lost
/// and how much the bleeding closes by itself.
/// </summary>
/// <param name="BleedAmount">The amount of blood the entity will lose.</param>
/// <param name="BleedReductionAmount">The amount of bleed reduction that will happen.</param>
[ByRefEvent]
public record struct BleedModifierEvent(float BleedAmount, float BleedReductionAmount);
