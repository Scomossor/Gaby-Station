// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.Antag;
using Content.Shared.Ghost;
using Content.Shared.StatusIcon.Components;
using Content.Shared.WhiteDream.BloodCult;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Content.Shared.WhiteDream.BloodCult.Components;
using Content.Shared.WhiteDream.BloodCult.Constructs;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Client.WhiteDream.BloodCult;

public sealed partial class BloodCultistSystem : EntitySystem
{
    private static readonly ResPath LeaderAuraRsi =
        new("/Textures/WhiteDream/BloodCult/Effects/leader_aura.rsi");

    private static readonly ResPath LeaderHaloRsi =
        new("/Textures/_Dumont/BloodCult/Effects/halo.rsi"); // Dumont

    private const string LeaderAuraState = "leader_aura";
    private const string LeaderHaloState = "halo"; // Dumont

    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IPlayerManager _player = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PentagramComponent, ComponentStartup>(OnPentagramAdded);
        SubscribeLocalEvent<PentagramComponent, ComponentShutdown>(OnPentagramRemoved);
        SubscribeLocalEvent<BloodCultLeaderComponent, ComponentStartup>(OnLeaderAdded);
        SubscribeLocalEvent<BloodCultLeaderComponent, ComponentShutdown>(OnLeaderRemoved);

        SubscribeLocalEvent<ConstructComponent, GetStatusIconsEvent>(GetCultStatusIcon);
        SubscribeLocalEvent<BloodCultistComponent, GetStatusIconsEvent>(GetBloodCultistStatusIcon);
        SubscribeLocalEvent<BloodCultLeaderComponent, GetStatusIconsEvent>(GetCultStatusIcon);
        SubscribeLocalEvent<BloodCultMarkComponent, GetStatusIconsEvent>(GetCultStatusIcon); // Dumont

        SubscribeLocalEvent<ConstructComponent, CanDisplayStatusIconsEvent>(OnCanShowCultIcon);
        SubscribeLocalEvent<BloodCultistComponent, CanDisplayStatusIconsEvent>(OnCanShowCultIcon);
        SubscribeLocalEvent<BloodCultLeaderComponent, CanDisplayStatusIconsEvent>(OnCanShowCultIcon);
        SubscribeLocalEvent<BloodCultMarkComponent, CanDisplayStatusIconsEvent>(OnCanShowCultIcon); // Dumont
    }

    private void GetCultStatusIcon<T>(Entity<T> ent, ref GetStatusIconsEvent args)
        where T : IComponent, IAntagStatusIconComponent
    {
        var canEv = new CanDisplayStatusIconsEvent(_player.LocalSession?.AttachedEntity);
        RaiseLocalEvent(ent, ref canEv);

        if (canEv.Cancelled || !_prototype.TryIndex(ent.Comp.StatusIcon, out var icon))
            return;

        args.StatusIcons.Add(icon);
    }

    private void GetBloodCultistStatusIcon(Entity<BloodCultistComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<BloodCultLeaderComponent>(ent))
            return;

        GetCultStatusIcon(ent, ref args);
    }

    private void OnPentagramAdded(EntityUid uid, PentagramComponent component, ComponentStartup args) =>
        RefreshPentagramVisual(uid, component); // Dumont

    // Dumont
    private void RefreshPentagramVisual(EntityUid uid, PentagramComponent component)
    {
        if (HasComp<BloodCultLeaderComponent>(uid))
        {
            RemovePentagram(uid);
            AddLeaderHalo(uid);
            RefreshLeaderAura(uid);
            return;
        }

        RemoveLeaderHalo(uid);
        RemoveLeaderAura(uid);
        AddPentagram(uid, component);
    }

    private void AddPentagram(EntityUid uid, PentagramComponent component)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!sprite.LayerMapTryGet(PentagramKey.Key, out _))
        {
            var randomState = _random.Pick(component.States);

            var layer = sprite.AddLayer(new SpriteSpecifier.Rsi(component.RsiPath, randomState));

            sprite.LayerMapSet(PentagramKey.Key, layer);
            sprite.LayerSetOffset(layer, component.Offset); // Dumont
        }
    }

    private void OnPentagramRemoved(EntityUid uid, PentagramComponent component, ComponentShutdown args)
    {
        RemovePentagram(uid); // Dumont
        RemoveLeaderHalo(uid); // Dumont
        RemoveLeaderAura(uid);
    }

    private void OnLeaderAdded(EntityUid uid, BloodCultLeaderComponent component, ComponentStartup args)
    {
        if (TryComp<PentagramComponent>(uid, out var pentagram))
            RefreshPentagramVisual(uid, pentagram); // Dumont
    }

    private void OnLeaderRemoved(EntityUid uid, BloodCultLeaderComponent component, ComponentShutdown args)
    {
        RemoveLeaderHalo(uid); // Dumont
        RemoveLeaderAura(uid);

        if (!TerminatingOrDeleted(uid) && TryComp<PentagramComponent>(uid, out var pentagram))
            AddPentagram(uid, pentagram); // Dumont
    }

    private void RemovePentagram(EntityUid uid)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) ||
            !sprite.LayerMapTryGet(PentagramKey.Key, out var layer))
            return;

        sprite.RemoveLayer(layer);
    }

    private void AddLeaderHalo(EntityUid uid)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) ||
            sprite.LayerMapTryGet(BloodCultVisualLayers.LeaderHalo, out _))
            return;

        var layer = sprite.AddLayer(new SpriteSpecifier.Rsi(LeaderHaloRsi, LeaderHaloState));
        sprite.LayerMapSet(BloodCultVisualLayers.LeaderHalo, layer);
    }

    private void RemoveLeaderHalo(EntityUid uid)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) ||
            !sprite.LayerMapTryGet(BloodCultVisualLayers.LeaderHalo, out var layer))
            return;

        sprite.RemoveLayer(layer);
    }

    private void RefreshLeaderAura(EntityUid uid)
    {
        if (!HasComp<BloodCultLeaderComponent>(uid) || !HasComp<PentagramComponent>(uid))
        {
            RemoveLeaderAura(uid);
            return;
        }

        if (!TryComp<SpriteComponent>(uid, out var sprite) ||
            sprite.LayerMapTryGet(BloodCultVisualLayers.LeaderAura, out _))
            return;

        var layer = sprite.AddLayer(new SpriteSpecifier.Rsi(LeaderAuraRsi, LeaderAuraState));
        sprite.LayerMapSet(BloodCultVisualLayers.LeaderAura, layer);
    }

    private void RemoveLeaderAura(EntityUid uid)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) ||
            !sprite.LayerMapTryGet(BloodCultVisualLayers.LeaderAura, out var layer))
            return;

        sprite.RemoveLayer(layer);
    }

    /// <summary>
    /// Determine whether a client should display the cult icon.
    /// </summary>
    private void OnCanShowCultIcon<T>(EntityUid uid, T comp, ref CanDisplayStatusIconsEvent args)
        where T : IAntagStatusIconComponent
    {
        if (!CanDisplayIcon(args.User, comp.IconVisibleToGhost))
            args.Cancelled = true;
    }

    /// <summary>
    /// The criteria that determine whether a client should see Cult/Cult leader icons.
    /// </summary>
    private bool CanDisplayIcon(EntityUid? uid, bool visibleToGhost)
    {
        if (HasComp<BloodCultistComponent>(uid) || HasComp<BloodCultLeaderComponent>(uid) ||
            HasComp<ConstructComponent>(uid))
            return true;

        return visibleToGhost && HasComp<GhostComponent>(uid);
    }
}

internal enum BloodCultVisualLayers : byte
{
    LeaderHalo, // Dumont
    LeaderAura,
}
