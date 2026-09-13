// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Orion.Power.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InducerComponent : Component
{
    [DataField, AutoNetworkedField]
    public string PowerCellSlotId = "inducer_power_cell_slot";

    [DataField, AutoNetworkedField]
    public int TransferRate;

    [DataField, AutoNetworkedField]
    public List<int> AvailableTransferRates = new();

    [DataField, AutoNetworkedField]
    public bool CombatInducer;

    [DataField]
    public float TransferDelay;

    /// <summary>
    ///     Multiply transferring energy for non-anchored entities (weapons, batteries, clothing, etc.).
    /// </summary>
    [DataField]
    public float TransferMultiplier;

    /// <summary>
    ///     Multiply transferring energy for machines, only fucking machines!!!
    /// </summary>
    [DataField]
    public float StructureTransferMultiplier;

    [DataField]
    public float MaxDistance;
}

[Serializable, NetSerializable]
public sealed partial class InducerDoAfterEvent : SimpleDoAfterEvent;
