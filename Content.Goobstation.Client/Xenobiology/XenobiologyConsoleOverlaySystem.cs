// SPDX-FileCopyrightText: 2026 Project Dumont
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.Xenobiology.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Goobstation.Client.Xenobiology;

public sealed partial class XenobiologyConsoleOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IPlayerManager _player = default!;

    private XenobiologyConsoleOverlay _consoleOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _consoleOverlay = new();

        SubscribeLocalEvent<XenobiologyConsoleViewComponent, ComponentInit>(OnViewInit);
        SubscribeLocalEvent<XenobiologyConsoleViewComponent, ComponentRemove>(OnViewRemove);
        SubscribeLocalEvent<XenobiologyConsoleViewComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<XenobiologyConsoleViewComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        RemoveOverlay();
        _consoleOverlay.Dispose();
    }

    private void OnViewInit(Entity<XenobiologyConsoleViewComponent> ent, ref ComponentInit args)
    {
        if (_player.LocalEntity == ent.Owner)
            AddOverlay();
    }

    private void OnViewRemove(Entity<XenobiologyConsoleViewComponent> ent, ref ComponentRemove args)
    {
        if (_player.LocalEntity == ent.Owner)
            RemoveOverlay();
    }

    private void OnPlayerAttached(Entity<XenobiologyConsoleViewComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        AddOverlay();
    }

    private void OnPlayerDetached(Entity<XenobiologyConsoleViewComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveOverlay();
    }

    private void AddOverlay()
    {
        if (!_overlay.HasOverlay<XenobiologyConsoleOverlay>())
            _overlay.AddOverlay(_consoleOverlay);
    }

    private void RemoveOverlay()
    {
        _overlay.RemoveOverlay<XenobiologyConsoleOverlay>();
        _consoleOverlay.ReleaseResources();
    }
}
