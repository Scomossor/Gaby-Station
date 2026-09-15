// SPDX-FileCopyrightText: 2026 Project Dumont
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Numerics;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Goobstation.Shared.SurveillanceCamera;

/// <summary>
/// Keeps console camera line-of-sight checks identical on the client and server.
/// </summary>
public sealed partial class SharedConsoleCameraSystem : EntitySystem
{
    private static readonly Vector2[] SightOriginOffsets =
    {
        new(0.45f, 0f),
        new(-0.45f, 0f),
        new(0f, 0.45f),
        new(0f, -0.45f),
    };

    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private readonly HashSet<EntityUid> _intersectingEntities = [];

    public bool TryCreateVision(
        EntityUid camera,
        ConsoleCameraComponent component,
        out ConsoleCameraVision vision)
    {
        var xform = Transform(camera);
        if (xform.GridUid is not { } gridUid ||
            !TryComp<MapGridComponent>(gridUid, out var grid))
        {
            vision = default;
            return false;
        }

        var cameraTile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        var cameraPosition = _transform.GetWorldPosition(xform);
        var origins = new List<MapCoordinates>
        {
            new(cameraPosition, xform.MapID),
        };

        foreach (var offset in SightOriginOffsets)
        {
            var origin = new MapCoordinates(cameraPosition + offset, xform.MapID);
            var originTile = _map.TileIndicesFor(gridUid, grid, origin);
            if (originTile == cameraTile || IsTileOpaque(gridUid, originTile))
                continue;

            origins.Add(origin);
        }

        vision = new ConsoleCameraVision(gridUid, cameraTile, component.Range, origins);
        return true;
    }

    public bool IsVisible(in ConsoleCameraVision vision, EntityCoordinates target)
    {
        if (!target.IsValid(EntityManager) ||
            !TryComp<MapGridComponent>(vision.Grid, out var grid))
        {
            return false;
        }

        var mapCoordinates = _transform.ToMapCoordinates(target);
        var targetTile = _map.TileIndicesFor(vision.Grid, grid, mapCoordinates);
        return IsVisible(vision, mapCoordinates, targetTile);
    }

    public bool IsVisible(
        in ConsoleCameraVision vision,
        MapCoordinates target,
        Vector2i targetTile)
    {
        var cameraGrid = vision.Grid;
        var cameraTile = vision.CameraTile;

        foreach (var origin in vision.Origins)
        {
            if (origin.MapId != target.MapId)
                continue;

            if (_interaction.InRangeUnobstructed(
                    origin,
                    target,
                    vision.Range,
                    CollisionGroup.Opaque,
                    blocker => ShouldIgnoreBlocker(blocker, cameraGrid, cameraTile, targetTile)))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsTileOpaque(EntityUid gridUid, Vector2i tile)
    {
        _intersectingEntities.Clear();
        _lookup.GetLocalEntitiesIntersecting(
            gridUid,
            tile,
            _intersectingEntities,
            0f,
            LookupFlags.Static | LookupFlags.Approximate);

        foreach (var entity in _intersectingEntities)
        {
            if (!TryComp<Robust.Shared.Physics.FixturesComponent>(entity, out var fixtures))
                continue;

            foreach (var fixture in fixtures.Fixtures.Values)
            {
                if ((fixture.CollisionLayer & (int) CollisionGroup.Opaque) != 0)
                    return true;
            }
        }

        return false;
    }

    private bool ShouldIgnoreBlocker(
        EntityUid uid,
        EntityUid cameraGrid,
        Vector2i cameraTile,
        Vector2i targetTile)
    {
        if (!TryComp(uid, out TransformComponent? xform) || !xform.Anchored)
            return true;

        if (xform.GridUid != cameraGrid ||
            !TryComp<MapGridComponent>(cameraGrid, out var grid))
        {
            return false;
        }

        var blockerTile = _map.TileIndicesFor(cameraGrid, grid, xform.Coordinates);
        return blockerTile == cameraTile || blockerTile == targetTile;
    }
}

public readonly record struct ConsoleCameraVision(
    EntityUid Grid,
    Vector2i CameraTile,
    float Range,
    IReadOnlyList<MapCoordinates> Origins);
