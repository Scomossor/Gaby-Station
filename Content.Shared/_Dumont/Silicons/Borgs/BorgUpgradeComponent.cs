// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Dumont.Silicons.Borgs;

/// <summary>
/// disparado quando a instalação do aprimoramento termina.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class BorgUpgradeDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// item que melhora um chassi de borg de forma permanente, instalado pelo painel aberto
/// é consumido na instalação e não volta
/// </summary>
[RegisterComponent]
public sealed partial class BorgUpgradeComponent : Component
{
    /// <summary>
    /// aparece ao examinar um borg aprimorado
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// componentes dados ao borg. pode ficar vazio em aprimoramento que só mexe em
    /// <see cref="ExtraModules"/>.
    /// </summary>
    [DataField]
    public ComponentRegistry Components = new();

    /// <summary>
    /// slots de módulo a mais liberados na instalação
    /// </summary>
    [DataField]
    public int ExtraModules;

    /// <summary>
    /// quanto tempo leva pra instalar
    /// </summary>
    [DataField]
    public TimeSpan InstallTime = TimeSpan.FromSeconds(3);
}

/// <summary>
/// guarda os aprimoramentos que o borg já tem, pra não instalar o mesmo duas vezes e pra
/// robótica ver o que o chassi está carregando.
/// </summary>
[RegisterComponent]
public sealed partial class BorgUpgradedComponent : Component
{
    [DataField]
    public List<LocId> Installed = new();
}
