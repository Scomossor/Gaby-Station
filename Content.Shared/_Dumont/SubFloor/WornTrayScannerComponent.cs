// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Dumont.SubFloor;

/// <summary>
/// marca um scanner de subsolo que só funciona vestido, acionado por ItemToggle
/// em vez do clique do scanner de mão. componente próprio porque a engine não
/// aceita dois sistemas na mesma dupla de componente e evento
/// </summary>
[RegisterComponent]
public sealed partial class WornTrayScannerComponent : Component;
