// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Shared._DV.CosmicCult.Components.Examine;

/// <summary>
/// Marker component for The Unknown. We also use this to detect its spawn through CultRule!
/// </summary>

using Robust.Shared.GameStates;
[RegisterComponent, NetworkedComponent]
public sealed partial class CosmicGodComponent : Component;
