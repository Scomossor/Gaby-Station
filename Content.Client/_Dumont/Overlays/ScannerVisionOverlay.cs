// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared._Dumont.Overlays;
using Content.Shared.NodeContainer;
using Content.Shared.SubFloor;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Client._Dumont.Overlays;

/// <summary>
/// redesenha o equipamento de engenharia por cima da máscara de campo de visão.
/// overlay de WorldSpace roda depois do jogo pintar o escuro, então o que a
/// gente desenhar aqui aparece através da parede.. nada é tingido, é o sprite
/// real da entidade sendo desenhado de novo por cima
/// </summary>
public sealed class ScannerVisionOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private readonly SharedTransformSystem _transform;
    private readonly SpriteSystem _sprite;
    private readonly EntityLookupSystem _lookup;

    private readonly HashSet<EntityUid> _found = new();

    public ScannerVisionComponent? Comp;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public ScannerVisionOverlay()
    {
        IoCManager.InjectDependencies(this);

        // equipamento vem por cima da planta do meson
        ZIndex = 1;

        _transform = _entity.System<SharedTransformSystem>();
        _sprite = _entity.System<SpriteSystem>();
        _lookup = _entity.System<EntityLookupSystem>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return args.Viewport.Eye == _eye.CurrentEye && Comp is { Structure: true };
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (Comp is not { } comp || args.Viewport.Eye is not { } eye)
            return;

        if (_player.LocalEntity is not { } player
            || !_entity.TryGetComponent(player, out TransformComponent? playerXform))
            return;

        var handle = args.WorldHandle;
        var mapPos = _transform.GetMapCoordinates(player, playerXform);

        _found.Clear();
        _lookup.GetEntitiesInRange(mapPos.MapId, mapPos.Position, comp.Range, _found,
            LookupFlags.Static | LookupFlags.Sundries | LookupFlags.Approximate);

        var spriteQuery = _entity.GetEntityQuery<SpriteComponent>();
        var xformQuery = _entity.GetEntityQuery<TransformComponent>();
        var subfloorQuery = _entity.GetEntityQuery<SubFloorHideComponent>();
        var nodeQuery = _entity.GetEntityQuery<NodeContainerComponent>();

        foreach (var uid in _found)
        {
            // subsolo pega cano, fio e lixeira enterrados; nó de rede pega o
            // maquinário à vista, que é tudo que liga em energia ou atmos
            if (!subfloorQuery.HasComponent(uid) && !nodeQuery.HasComponent(uid))
                continue;

            if (!spriteQuery.TryGetComponent(uid, out var sprite) || !sprite.Visible)
                continue;

            if (!xformQuery.TryGetComponent(uid, out var xform) || xform.MapID != mapPos.MapId)
                continue;

            var position = _transform.GetWorldPosition(xform);
            var rotation = _transform.GetWorldRotation(xform);

            _sprite.RenderSprite((uid, sprite), handle, eye.Rotation, rotation, position);
        }

        handle.SetTransform(Matrix3x2.Identity);
    }
}
