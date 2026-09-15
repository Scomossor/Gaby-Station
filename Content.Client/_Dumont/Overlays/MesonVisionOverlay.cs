// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.Doors.Components;
using Content.Shared.Examine;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Client._Dumont.Overlays;

/// <summary>
/// desenha a estação escondida atrás das paredes, dentro de um alcance.
/// o campo de visão do jogo é global (liga ou desliga a tela inteira), então
/// desligar ele revelaria a estação toda.. mantendo ele ligado e redesenhando
/// aqui, fora do alcance tudo continua normal, com sombra e escuro no lugar
///
/// desenha chão, parede, porta e janela juntos: só o chão vira laje solta
/// boiando no preto, com buraco onde deveria ter parede
/// </summary>
public sealed class MesonVisionOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IResourceCache _resource = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDefs = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly SharedTransformSystem _transform;
    private readonly SharedMapSystem _map;
    private readonly SpriteSystem _sprite;
    private readonly EntityLookupSystem _lookup;
    private readonly ExamineSystemShared _examine;
    private readonly TagSystem _tag;

    private const int TilePixels = 32;

    private static readonly ProtoId<TagPrototype> WindowTag = "Window";

    private List<Entity<MapGridComponent>> _grids = new();
    private readonly HashSet<EntityUid> _found = new();

    // oclusão custa raycast, então guarda o resultado e refaz de tempos em tempos
    private readonly List<(EntityUid Grid, Vector2i Indices, Tile Tile)> _hiddenTiles = new();
    private readonly List<EntityUid> _hiddenEnts = new();
    private TimeSpan _nextScan;
    private Vector2 _lastScanPos;

    public float Range = 8f;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public MesonVisionOverlay()
    {
        IoCManager.InjectDependencies(this);

        // a planta é o fundo: precisa sair antes do equipamento do t-ray, senão
        // o chão desenhado aqui cobre os canos. sem z definido a ordem entre
        // dois overlays do mesmo espaço é sorteada
        ZIndex = 0;

        _transform = _entity.System<SharedTransformSystem>();
        _map = _entity.System<SharedMapSystem>();
        _sprite = _entity.System<SpriteSystem>();
        _lookup = _entity.System<EntityLookupSystem>();
        _examine = _entity.System<ExamineSystemShared>();
        _tag = _entity.System<TagSystem>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return args.Viewport.Eye == _eye.CurrentEye;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.Viewport.Eye is not { } eye
            || _player.LocalEntity is not { } player
            || !_entity.TryGetComponent(player, out TransformComponent? playerXform))
            return;

        var handle = args.WorldHandle;
        var mapPos = _transform.GetMapCoordinates(player, playerXform);

        Refresh(player, mapPos);
        DrawTiles(handle);
        DrawEntities(handle, eye.Rotation, mapPos.MapId);

        handle.SetTransform(Matrix3x2.Identity);
    }

    private void DrawTiles(DrawingHandleWorld handle)
    {
        var gridQuery = _entity.GetEntityQuery<MapGridComponent>();

        foreach (var (gridUid, indices, tile) in _hiddenTiles)
        {
            if (!gridQuery.TryGetComponent(gridUid, out var grid))
                continue;

            if (!_tileDefs.TryGetDefinition(tile.TypeId, out var def) || def.Sprite is not { } spritePath)
                continue;

            var texture = _resource.GetResource<TextureResource>(spritePath).Texture;
            var variant = def.Variants == 0 ? 0 : tile.Variant % def.Variants;
            var region = UIBox2.FromDimensions(variant * TilePixels, 0, TilePixels, TilePixels);

            var local = Matrix3x2.CreateTranslation(new Vector2(indices.X, indices.Y) * grid.TileSize);
            handle.SetTransform(local * _transform.GetWorldMatrix(gridUid));
            handle.DrawTextureRectRegion(texture, new Box2(Vector2.Zero, new Vector2(grid.TileSize)), null, region);
        }
    }

    private void DrawEntities(DrawingHandleWorld handle, Angle eyeRot, MapId mapId)
    {
        handle.SetTransform(Matrix3x2.Identity);

        var spriteQuery = _entity.GetEntityQuery<SpriteComponent>();
        var xformQuery = _entity.GetEntityQuery<TransformComponent>();

        foreach (var uid in _hiddenEnts)
        {
            if (!spriteQuery.TryGetComponent(uid, out var sprite) || !sprite.Visible)
                continue;

            if (!xformQuery.TryGetComponent(uid, out var xform) || xform.MapID != mapId)
                continue;

            _sprite.RenderSprite((uid, sprite),
                handle,
                eyeRot,
                _transform.GetWorldRotation(xform),
                _transform.GetWorldPosition(xform));
        }
    }

    private void Refresh(EntityUid player, MapCoordinates mapPos)
    {
        if (_timing.CurTime < _nextScan && (mapPos.Position - _lastScanPos).LengthSquared() < 0.25f)
            return;

        _nextScan = _timing.CurTime + TimeSpan.FromSeconds(0.1);
        _lastScanPos = mapPos.Position;

        _hiddenTiles.Clear();
        _hiddenEnts.Clear();

        var bounds = Box2.CenteredAround(mapPos.Position, new Vector2(Range * 2));

        _grids.Clear();
        _mapManager.FindGridsIntersecting(mapPos.MapId, bounds, ref _grids);

        foreach (var grid in _grids)
        {
            foreach (var tileRef in _map.GetTilesIntersecting(grid.Owner, grid.Comp, bounds))
            {
                if (tileRef.Tile.IsEmpty)
                    continue;

                var center = _map.GridTileToWorld(grid.Owner, grid.Comp, tileRef.GridIndices);

                // a janela de varredura e quadrada de proposito, nao um circulo.
                // o InRangeUnOccluded devolve falso tanto pra bloqueado quanto pra
                // longe demais, entao o tile de canto entra aqui e ganha um pouco
                // mais de claridade. isso e o que da a leitura de scanner ligado,
                // cortar certinho no circulo deixa uma borda dura que parece bug
                if (_examine.InRangeUnOccluded(mapPos, center, Range, null))
                    continue;

                _hiddenTiles.Add((grid.Owner, tileRef.GridIndices, tileRef.Tile));
            }
        }

        _found.Clear();
        _lookup.GetEntitiesInRange(mapPos.MapId, mapPos.Position, Range, _found,
            LookupFlags.Static | LookupFlags.Sundries | LookupFlags.Approximate);

        var xformQuery = _entity.GetEntityQuery<TransformComponent>();

        foreach (var uid in _found)
        {
            if (uid == player || !IsStructure(uid))
                continue;

            if (!xformQuery.TryGetComponent(uid, out var xform))
                continue;

            var pos = _transform.GetMapCoordinates(uid, xform);

            if (_examine.InRangeUnOccluded(mapPos, pos, Range, null))
                continue;

            _hiddenEnts.Add(uid);
        }
    }

    /// <summary>
    /// só a planta da estação atravessa a parede: parede, porta e janela.
    /// equipamento de engenharia fica de fora de propósito, é trabalho do t-ray
    /// e assim cada óculos tem função própria.. porta e janela precisam de
    /// identificação separada porque vidro não bloqueia visão, então não tem
    /// oclusor e viraria buraco no cômodo revelado
    /// </summary>
    private bool IsStructure(EntityUid uid)
    {
        return _entity.HasComponent<OccluderComponent>(uid)
               || _entity.HasComponent<DoorComponent>(uid)
               || _tag.HasTag(uid, WindowTag);
    }
}
