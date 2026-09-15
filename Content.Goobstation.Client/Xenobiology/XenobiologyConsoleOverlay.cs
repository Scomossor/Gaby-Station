// SPDX-FileCopyrightText: 2026 Project Dumont
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client.SurveillanceCamera;
using Content.Goobstation.Shared.SurveillanceCamera;
using Content.Goobstation.Shared.Xenobiology.Components;
using Content.Shared.SurveillanceCamera;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Goobstation.Client.Xenobiology;

public sealed partial class XenobiologyConsoleOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> CameraStaticShader = "CameraStatic";
    private static readonly ProtoId<ShaderPrototype> StencilMaskShader = "StencilMask";
    private static readonly ProtoId<ShaderPrototype> StencilDrawShader = "StencilDraw";

    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private readonly EntityLookupSystem _lookup;
    private readonly SharedAppearanceSystem _appearance;
    private readonly SharedMapSystem _map;
    private readonly SharedTransformSystem _transform;
    private readonly SharedConsoleCameraSystem _cameraVision;

    private readonly HashSet<Entity<ConsoleCameraComponent>> _cameras = [];
    private readonly HashSet<(EntityUid Grid, Vector2i Tile)> _visibleTiles = [];

    private IRenderTexture? _staticTexture;
    private IRenderTexture? _stencilTexture;
    private float _visionUpdateAccumulator;

    private const float VisionUpdateRate = 1f / 15f;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public XenobiologyConsoleOverlay()
    {
        IoCManager.InjectDependencies(this);

        _lookup = _entManager.System<EntityLookupSystem>();
        _appearance = _entManager.System<SharedAppearanceSystem>();
        _map = _entManager.System<SharedMapSystem>();
        _transform = _entManager.System<SharedTransformSystem>();
        _cameraVision = _entManager.System<SharedConsoleCameraSystem>();
    }

    public void ReleaseResources()
    {
        _staticTexture?.Dispose();
        _staticTexture = null;
        _stencilTexture?.Dispose();
        _stencilTexture = null;
        _cameras.Clear();
        _visibleTiles.Clear();
        _visionUpdateAccumulator = 0f;
    }

    protected override void DisposeBehavior()
    {
        base.DisposeBehavior();
        ReleaseResources();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_player.LocalEntity is not { } player ||
            !_entManager.TryGetComponent<XenobiologyConsoleViewComponent>(player, out var view))
        {
            return;
        }

        if (_stencilTexture?.Texture.Size != args.Viewport.Size)
        {
            _staticTexture?.Dispose();
            _stencilTexture?.Dispose();
            _stencilTexture = _clyde.CreateRenderTarget(args.Viewport.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "xenobiology-console-stencil");
            _staticTexture = _clyde.CreateRenderTarget(args.Viewport.Size, new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb), name: "xenobiology-console-static");
        }

        var worldHandle = args.WorldHandle;
        var worldBounds = args.WorldBounds;
        var worldAabb = args.WorldAABB;
        var mapId = args.MapId;
        var invMatrix = args.Viewport.GetWorldToLocalMatrix();
        _visionUpdateAccumulator -= (float) _timing.FrameTime.TotalSeconds;
        if (_visionUpdateAccumulator <= 0f)
        {
            _visionUpdateAccumulator = MathF.Max(0f, _visionUpdateAccumulator + VisionUpdateRate);
            _visibleTiles.Clear();
            _cameras.Clear();
            _lookup.GetEntitiesIntersecting(
                mapId,
                worldAabb.Enlarged(view.CameraOverlaySearchRange),
                _cameras);

            foreach (var camera in _cameras)
            {
                if (!camera.Comp.Tags.Contains(view.RequiredCameraTag) ||
                    !_entManager.TryGetComponent<TransformComponent>(camera, out var xform) ||
                    xform.MapID != mapId ||
                    xform.GridUid is not { } gridUid ||
                    !_entManager.TryGetComponent<MapGridComponent>(gridUid, out var grid) ||
                    !IsCameraActive(camera))
                {
                    continue;
                }

                if (!_cameraVision.TryCreateVision(camera, camera.Comp, out var vision))
                    continue;

                foreach (var tile in _map.GetTilesIntersecting(gridUid, grid, new Circle(_transform.GetWorldPosition(xform), camera.Comp.Range), ignoreEmpty: false))
                {
                    var tileCoords = _map.GridTileToLocal(gridUid, grid, tile.GridIndices);
                    if (!_cameraVision.IsVisible(vision, _transform.ToMapCoordinates(tileCoords), tile.GridIndices))
                        continue;

                    _visibleTiles.Add((gridUid, tile.GridIndices));
                }
            }
        }

        worldHandle.RenderInRenderTarget(_stencilTexture!, () =>
        {
            foreach (var (gridUid, tile) in _visibleTiles)
            {
                if (!_entManager.TryGetComponent<MapGridComponent>(gridUid, out var grid))
                    continue;

                var gridMatrix = _transform.GetWorldMatrix(gridUid);
                worldHandle.SetTransform(Matrix3x2.Multiply(gridMatrix, invMatrix));
                worldHandle.DrawRect(_lookup.GetLocalBounds(tile, grid.TileSize), Color.White);
            }
        }, Color.Transparent);

        worldHandle.RenderInRenderTarget(_staticTexture!, () =>
        {
            worldHandle.SetTransform(invMatrix);
            worldHandle.UseShader(_proto.Index(CameraStaticShader).Instance());
            worldHandle.DrawRect(worldBounds, Color.White);
        }, Color.Black);

        worldHandle.UseShader(_proto.Index(StencilMaskShader).Instance());
        worldHandle.DrawTextureRect(_stencilTexture!.Texture, worldBounds);

        worldHandle.UseShader(_proto.Index(StencilDrawShader).Instance());
        worldHandle.DrawTextureRect(_staticTexture!.Texture, worldBounds);

        worldHandle.SetTransform(Matrix3x2.Identity);
        worldHandle.UseShader(null);
    }

    private bool IsCameraActive(EntityUid camera)
    {
        if (!_entManager.TryGetComponent<SurveillanceCameraVisualsComponent>(camera, out _) ||
            !_entManager.TryGetComponent<AppearanceComponent>(camera, out var appearance))
        {
            return false;
        }

        if (!_appearance.TryGetData(camera, SurveillanceCameraVisualsKey.Key, out SurveillanceCameraVisuals state, appearance))
            return false;

        return state is SurveillanceCameraVisuals.Active or SurveillanceCameraVisuals.InUse;
    }

}
