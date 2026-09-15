// SPDX-FileCopyrightText: 2026 Project Dumont
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Goobstation.Shared.Xenobiology;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Spawners;
using static Robust.Client.Animations.AnimationTrackProperty;

namespace Content.Goobstation.Client.Xenobiology;

public sealed partial class XenobiologyTransferAnimationSystem : EntitySystem
{
    private const float TubeDeployTime = 0.1f;
    private const float TubeHoldTime = 0.2f;
    private const float TubeOffset = 1f;
    private const float TubeRetractedOffset = 1.5f;
    private const float TargetTravelDistance = 2f;
    private const float TargetTravelTime = 0.3f;
    private const float EffectLifetime = 0.5f;

    private static readonly EntProtoId TubePrototype = "XenobiologyTransferTubeEffect";

    [Dependency] private AnimationPlayerSystem _animations = default!;
    [Dependency] private SpriteSystem _sprites = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<XenobiologyTransferAnimationEvent>(OnTransferAnimation);
        SubscribeLocalEvent<XenobiologyTransferAnimationComponent, AnimationCompletedEvent>(OnAnimationCompleted);
        SubscribeLocalEvent<XenobiologyTransferAnimationComponent, ComponentShutdown>(OnAnimationShutdown);
    }

    private void OnTransferAnimation(XenobiologyTransferAnimationEvent ev)
    {
        var coordinates = GetCoordinates(ev.Coordinates);
        if (!coordinates.IsValid(EntityManager))
            return;

        SpawnTube(coordinates);

        foreach (var netTarget in ev.Targets)
        {
            if (!TryGetEntity(netTarget, out var target) ||
                !TryComp<SpriteComponent>(target, out var targetSprite))
            {
                continue;
            }

            SpawnTargetClone(target.Value, targetSprite, coordinates, ev.Type);
        }
    }

    private void SpawnTube(EntityCoordinates coordinates)
    {
        var tube = Spawn(TubePrototype, coordinates);
        if (!TryComp<SpriteComponent>(tube, out var sprite))
        {
            Del(tube);
            return;
        }

        PrepareEffect(tube);
        _animations.Play(tube, CreateTubeAnimation(sprite), "xenobiology-transfer-tube");
    }

    private void SpawnTargetClone(
        EntityUid target,
        SpriteComponent targetSprite,
        EntityCoordinates coordinates,
        XenobiologyTransferAnimationType type)
    {
        var clone = Spawn("clientsideclone", coordinates);
        if (!TryComp<SpriteComponent>(clone, out var cloneSprite))
        {
            Del(clone);
            return;
        }

        _sprites.CopySprite((target, targetSprite), (clone, cloneSprite));
        _sprites.SetVisible((clone, cloneSprite), true);

        var animation = EnsureComp<XenobiologyTransferAnimationComponent>(clone);
        if (type == XenobiologyTransferAnimationType.Release && targetSprite.Visible)
        {
            animation.HiddenEntity = target;
            _sprites.SetVisible((target, targetSprite), false);
        }

        EnsureComp<TimedDespawnComponent>(clone).Lifetime = EffectLifetime;
        _animations.Play(clone, CreateTargetAnimation(cloneSprite, type), "xenobiology-transfer-target");
    }

    private void PrepareEffect(EntityUid effect)
    {
        EnsureComp<XenobiologyTransferAnimationComponent>(effect);
        EnsureComp<TimedDespawnComponent>(effect).Lifetime = EffectLifetime;
    }

    private static Animation CreateTubeAnimation(SpriteComponent sprite)
    {
        return new Animation
        {
            Length = TimeSpan.FromSeconds(TubeDeployTime * 2 + TubeHoldTime),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new KeyFrame(new Vector2(0f, TubeRetractedOffset), 0f),
                        new KeyFrame(new Vector2(0f, TubeOffset), TubeDeployTime),
                        new KeyFrame(new Vector2(0f, TubeOffset), TubeDeployTime + TubeHoldTime),
                        new KeyFrame(new Vector2(0f, TubeRetractedOffset), TubeDeployTime * 2 + TubeHoldTime),
                    },
                },
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new KeyFrame(sprite.Color.WithAlpha(0f), 0f),
                        new KeyFrame(sprite.Color, TubeDeployTime),
                        new KeyFrame(sprite.Color, TubeDeployTime + TubeHoldTime),
                        new KeyFrame(sprite.Color.WithAlpha(0f), TubeDeployTime * 2 + TubeHoldTime),
                    },
                },
            },
        };
    }

    private static Animation CreateTargetAnimation(
        SpriteComponent sprite,
        XenobiologyTransferAnimationType type)
    {
        var travel = new Vector2(0f, TargetTravelDistance);
        var startOffset = type == XenobiologyTransferAnimationType.Suction
            ? sprite.Offset
            : sprite.Offset + travel;
        var endOffset = type == XenobiologyTransferAnimationType.Suction
            ? sprite.Offset + travel
            : sprite.Offset;
        var startColor = type == XenobiologyTransferAnimationType.Suction
            ? sprite.Color
            : sprite.Color.WithAlpha(0f);
        var endColor = type == XenobiologyTransferAnimationType.Suction
            ? sprite.Color.WithAlpha(0f)
            : sprite.Color;

        return new Animation
        {
            Length = TimeSpan.FromSeconds(TargetTravelTime),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new KeyFrame(startOffset, 0f),
                        new KeyFrame(endOffset, TargetTravelTime),
                    },
                },
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new KeyFrame(startColor, 0f),
                        new KeyFrame(endColor, TargetTravelTime),
                    },
                },
            },
        };
    }

    private void OnAnimationCompleted(
        Entity<XenobiologyTransferAnimationComponent> ent,
        ref AnimationCompletedEvent args)
    {
        Del(ent);
    }

    private void OnAnimationShutdown(
        Entity<XenobiologyTransferAnimationComponent> ent,
        ref ComponentShutdown args)
    {
        if (ent.Comp.HiddenEntity is not { } hidden ||
            !TryComp<SpriteComponent>(hidden, out var sprite))
        {
            return;
        }

        _sprites.SetVisible((hidden, sprite), true);
        ent.Comp.HiddenEntity = null;
    }
}

[RegisterComponent]
[Access(typeof(XenobiologyTransferAnimationSystem))]
public sealed partial class XenobiologyTransferAnimationComponent : Component
{
    public EntityUid? HiddenEntity;
}
