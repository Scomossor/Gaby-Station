// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Dumont.Overlays;

/// <summary>
/// visão de scanner dos óculos de engenharia. redesenha os equipamentos de
/// engenharia por cima da máscara de campo de visão, então cano e fio aparecem
/// através das paredes.. nada é tingido, é o sprite real desenhado de novo
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ScannerVisionComponent : Component
{
    /// <summary>
    /// o que esse óculos sabe fazer, definido no prototype
    /// </summary>
    [DataField]
    public bool ShowsStructure;

    /// <summary>
    /// estado ligado/desligado, controlado pelo ItemToggle
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Structure;

    /// <summary>
    /// alcance em tiles, no mesmo espírito do scanner t-ray de mão
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 6f;

}
