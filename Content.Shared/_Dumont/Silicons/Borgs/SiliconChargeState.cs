// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._Dumont.Silicons.Borgs;

[Serializable, NetSerializable]
public enum SiliconChargeLevel : byte
{
    None,
    Critical,
    Low,
    Half,
    High,
    Full,
}

/// <summary>
/// visão aproximada da célula de energia de um silicon, replicada pro cliente
/// o BatteryComponent só existe no servidor, então o HUD na cabeça de outra pessoa não teria
/// como ver a carga.. vai só a faixa, nunca o número exato
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SiliconChargeStateComponent : Component
{
    [DataField, AutoNetworkedField]
    public SiliconChargeLevel Level = SiliconChargeLevel.Full;
}

/// <summary>
/// deixa enxergar os ícones de <see cref="SiliconChargeStateComponent"/>. é vestido, não nato.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShowSiliconChargeIconsComponent : Component;

/// <summary>
/// ícones de carga de silicon, mostrados no HUD de diagnóstico
/// </summary>
[Prototype]
public sealed partial class SiliconChargeIconPrototype : StatusIconPrototype, IInheritingPrototype
{
    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<SiliconChargeIconPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc />
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }
}
