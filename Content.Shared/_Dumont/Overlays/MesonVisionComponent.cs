// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Dumont.Overlays;

/// <summary>
/// visão de meson: desliga o campo de visão e esconde o que não é estrutura.
/// o upstream desenha os tiles escondidos com shader, mas nosso engine não expõe
/// a textura do campo de visão, e aproximar isso na CPU vira mancha na tela.
/// tirar o campo de visão e esconder gente e item entrega o mesmo resultado com
/// a estação na aparência normal, sem tingir nada
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class MesonVisionComponent : Component
{
    /// <summary>
    /// ligado pelo ItemToggle
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled;

    /// <summary>
    /// até onde esconder o que está atrás de parede, em tiles. fora disso o
    /// campo de visão desligado já não mostra nada de qualquer jeito
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 12f;
}
