// SPDX-FileCopyrightText: 2021 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2021 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2021 tmtmtl30 <53132901+tmtmtl30@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 TekuNut <13456422+TekuNut@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 keronshb <54602815+keronshb@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Kara <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Pieter-Jan Briers <pieterjan.briers@gmail.com>
// SPDX-FileCopyrightText: 2023 TemporalOroboros <TemporalOroboros@gmail.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Jake Huxell <JakeHuxell@pm.me>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GabyChangelog <agentepanela2@gmail.com>
// SPDX-FileCopyrightText: 2025 Hannah Giovanna Dawson <karakkaraz@gmail.com>
// SPDX-FileCopyrightText: 2025 Rouden <149893554+Roudenn@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 SX-7 <sn1.test.preria.2002@gmail.com>
// SPDX-FileCopyrightText: 2025 Tayrtahn <tayrtahn@gmail.com>
// SPDX-FileCopyrightText: 2025 slarticodefast <161409025+slarticodefast@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.SprayPainter.Prototypes;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Doors;

public sealed class DoorSystem : SharedDoorSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationSystem = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DoorComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<DoorComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    protected override void OnComponentInit(Entity<DoorComponent> ent, ref ComponentInit args)
    {
        var comp = ent.Comp;
        comp.OpenSpriteStates = new List<(Enum, string)>(2);
        comp.ClosedSpriteStates = new List<(Enum, string)>(2);

        comp.OpenSpriteStates.Add((DoorVisualLayers.Base, comp.OpenSpriteState));
        comp.ClosedSpriteStates.Add((DoorVisualLayers.Base, comp.ClosedSpriteState));

        comp.OpeningAnimation = new Animation
        {
            Length = TimeSpan.FromSeconds(comp.OpeningAnimationTime),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = DoorVisualLayers.Base,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(comp.OpeningSpriteState, 0f),
                    },
                },
            },
        };

        comp.ClosingAnimation = new Animation
        {
            Length = TimeSpan.FromSeconds(comp.ClosingAnimationTime),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = DoorVisualLayers.Base,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(comp.ClosingSpriteState, 0f),
                    },
                },
            },
        };

        comp.EmaggingAnimation = new Animation
        {
            Length = TimeSpan.FromSeconds(comp.EmaggingAnimationTime),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick
                {
                    LayerKey = DoorVisualLayers.BaseUnlit,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(comp.EmaggingSpriteState, 0f),
                    },
                },
            },
        };
    }

    private void OnAnimationCompleted(Entity<DoorComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key != DoorComponent.OpenKey && args.Key != DoorComponent.CloseKey)
            return;

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

       var doorSpriteStates = ent.Comp.State == DoorState.Open ? ent.Comp.OpenSpriteStates : ent.Comp.ClosedSpriteStates;
       
       foreach (var (layer, layerState) in doorSpriteStates)
       {
          _sprite.LayerSetAutoAnimated((ent.Owner, sprite), layer, true);
          _sprite.LayerSetRsiState((ent.Owner, sprite), layer, layerState);
       }
    }

    private void OnAppearanceChange(Entity<DoorComponent> entity, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<DoorState>(entity, DoorVisuals.State, out var state, args.Component))
            state = DoorState.Closed;

        if (AppearanceSystem.TryGetData<string>(entity, PaintableVisuals.Prototype, out var prototype, args.Component))
            UpdateSpriteLayers((entity.Owner, args.Sprite), prototype);

        UpdateAppearanceForDoorState(entity, args.Sprite, state);
    }

    private void UpdateAppearanceForDoorState(Entity<DoorComponent> entity, SpriteComponent sprite, DoorState state)
    {
        _sprite.SetDrawDepth((entity.Owner, sprite), state is DoorState.Open ? entity.Comp.OpenDrawDepth : entity.Comp.ClosedDrawDepth);

        switch (state)
        {
            case DoorState.Open:
                if (_animationSystem.HasRunningAnimation(entity, DoorComponent.OpenKey))
                    return;

                if (_animationSystem.HasRunningAnimation(entity, DoorComponent.CloseKey))
                {
                    _animationSystem.Stop(entity.Owner, DoorComponent.CloseKey);
                    _animationSystem.Play(entity, (Animation)entity.Comp.OpeningAnimation, DoorComponent.OpenKey);
                }

                foreach (var (layer, layerState) in entity.Comp.OpenSpriteStates)
                {
                    // Allow animations to play while it's open (e.g., pinion);
                    // the animation unsets this so we gotta set it again.
                    _sprite.LayerSetAutoAnimated((entity.Owner, sprite), layer, true);
                    _sprite.LayerSetRsiState((entity.Owner, sprite), layer, layerState);
                }

                return;
            case DoorState.Closed:
                if (_animationSystem.HasRunningAnimation(entity, DoorComponent.CloseKey))
                    return;

                if (_animationSystem.HasRunningAnimation(entity, DoorComponent.OpenKey))
                {
                    _animationSystem.Stop(entity.Owner, DoorComponent.OpenKey);
                    _animationSystem.Play(entity, (Animation)entity.Comp.OpeningAnimation, DoorComponent.CloseKey);
                }

                foreach (var (layer, layerState) in entity.Comp.ClosedSpriteStates)
                {
                    _sprite.LayerSetAutoAnimated((entity.Owner, sprite), layer, true);
                    _sprite.LayerSetRsiState((entity.Owner, sprite), layer, layerState);
                }

                return;
            case DoorState.Opening:
                if (entity.Comp.OpeningAnimationTime == 0.0)
                    return;

                if (_animationSystem.HasRunningAnimation(entity, DoorComponent.OpenKey))
                    return;

                if (_animationSystem.HasRunningAnimation(entity, DoorComponent.CloseKey))
                    _animationSystem.Stop(entity.Owner, DoorComponent.CloseKey);

                _animationSystem.Play(entity, (Animation)entity.Comp.OpeningAnimation, DoorComponent.OpenKey);

                return;
            case DoorState.Closing:
                if (entity.Comp.ClosingAnimationTime == 0.0)
                    return;

                if (_animationSystem.HasRunningAnimation(entity, DoorComponent.CloseKey))
                    return;

                if (_animationSystem.HasRunningAnimation(entity, DoorComponent.OpenKey))
                    _animationSystem.Stop(entity.Owner, DoorComponent.OpenKey);

                _animationSystem.Play(entity, (Animation)entity.Comp.ClosingAnimation, DoorComponent.CloseKey);

                return;
            case DoorState.Denying:
                if (_animationSystem.HasRunningAnimation(entity, DoorComponent.DenyKey))
                    return;

                _animationSystem.Play(entity, (Animation)entity.Comp.DenyingAnimation, DoorComponent.DenyKey);

                return;
            case DoorState.Emagging:
                if (_animationSystem.HasRunningAnimation(entity, DoorComponent.EmagKey))
                    return;

                _animationSystem.Play(entity, (Animation)entity.Comp.EmaggingAnimation, DoorComponent.EmagKey);

                return;
        }
    }

    private void UpdateSpriteLayers(Entity<SpriteComponent> sprite, string targetProto)
    {
        if (!_prototypeManager.TryIndex(targetProto, out var target))
            return;

        if (!target.TryGetComponent(out SpriteComponent? targetSprite, _componentFactory))
            return;

        _sprite.SetBaseRsi(sprite.AsNullable(), targetSprite.BaseRSI);
    }
}