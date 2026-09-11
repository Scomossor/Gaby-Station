// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Trauma.Shared.Popups;

/// <summary>
/// Popups for genetics code that runs in predicted paths.
/// </summary>
/// <remarks>
/// </remarks>
public sealed partial class TraumaPopupSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    /// <summary>
    /// Shows a popup above an entity to a single recipient, once.
    /// </summary>
    public void PopupEntity(string? message, EntityUid uid, EntityUid recipient, PopupType type = PopupType.Small)
    {
        if (_net.IsServer)
            _popup.PopupEntity(message, uid, recipient, type);
    }

    public void PopupEntity(string? recipientMessage, string? othersMessage, EntityUid uid, EntityUid recipient, PopupType type = PopupType.Small)
    {
        if (!_net.IsServer)
            return;

        _popup.PopupEntity(recipientMessage, uid, recipient, type);
        _popup.PopupEntity(othersMessage, uid, Filter.PvsExcept(recipient, entityManager: EntityManager), true, type);
    }
}
