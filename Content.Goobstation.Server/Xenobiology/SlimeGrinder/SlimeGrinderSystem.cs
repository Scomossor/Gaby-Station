// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 TheBorzoiMustConsume <197824988+TheBorzoiMustConsume@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Goobstation.Shared.Xenobiology.Components;
using Content.Goobstation.Shared.Xenobiology.Systems;
using Content.Server.Power.Components;
using Content.Shared.Audio;
using Content.Shared.Climbing.Events;
using Content.Shared.Construction.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Jittering;
using Content.Shared.Medical;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Power;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Server.Containers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Goobstation.Server.Xenobiology.SlimeGrinder;

public sealed partial class SlimeGrinderSystem : EntitySystem
{
    [Dependency] private readonly XenobiologySystem _xenobio = default!;
    [Dependency] private readonly SharedJitteringSystem _jitteringSystem = default!;
    [Dependency] private readonly SharedAudioSystem _sharedAudioSystem = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly ContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlimeGrinderComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SlimeGrinderComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ActiveSlimeGrinderComponent, ComponentInit>(OnActiveInit);
        SubscribeLocalEvent<ActiveSlimeGrinderComponent, ComponentRemove>(OnActiveShutdown);
        SubscribeLocalEvent<ActiveSlimeGrinderComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        SubscribeLocalEvent<SlimeGrinderComponent, ClimbedOnEvent>(OnClimbedOn);
        SubscribeLocalEvent<SlimeGrinderComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<SlimeGrinderComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SlimeGrinderComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
        SubscribeLocalEvent<SlimeGrinderComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SlimeGrinderComponent, ReclaimerDoAfterEvent>(OnDoAfter);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveSlimeGrinderComponent, SlimeGrinderComponent>();
        while (query.MoveNext(out var uid, out _, out var grinder))
        {
            grinder.ProcessingTimer = Math.Clamp(grinder.ProcessingTimer - frameTime, 0, grinder.ProcessingTimer);

            if (grinder.ProcessingTimer > 0)
                continue;

            foreach (var yield in grinder.YieldQueue)
            {
                for (int i = 0; i < yield.Value; i++)
                    SpawnNextToOrDrop(yield.Key, uid);
            }

            grinder.YieldQueue.Clear();

            RemCompDeferred<ActiveSlimeGrinderComponent>(uid);
        }

    }

    #region  Active Grinding

    private void OnStartup(Entity<SlimeGrinderComponent> grinder, ref ComponentStartup args)
    {
        grinder.Comp.SlimeContainer =
            _container.EnsureContainer<Container>(grinder, SlimeGrinderComponent.SlimeContainerId);
    }

    private void OnShutdown(Entity<SlimeGrinderComponent> grinder, ref ComponentShutdown args)
    {
        if (grinder.Comp.SlimeContainer is null ||
            grinder.Comp.SlimeContainer.Owner == EntityUid.Invalid)
            return;

        _container.EmptyContainer(grinder.Comp.SlimeContainer, destination: Transform(grinder).Coordinates);
    }

    private void OnActiveInit(Entity<ActiveSlimeGrinderComponent> activeGrinder, ref ComponentInit args)
    {
        if (!TryComp<SlimeGrinderComponent>(activeGrinder, out var grinder))
            return;

        _jitteringSystem.AddJitter(activeGrinder, -10, 100);
        _sharedAudioSystem.PlayPvs(grinder.GrindSound, activeGrinder);
        _ambientSoundSystem.SetAmbience(activeGrinder, true);
    }

    private void OnActiveShutdown(Entity<ActiveSlimeGrinderComponent> activeGrinder, ref ComponentRemove args)
    {
        RemComp<JitteringComponent>(activeGrinder);
        _ambientSoundSystem.SetAmbience(activeGrinder, false);
    }

    private void OnUnanchorAttempt(Entity<ActiveSlimeGrinderComponent> activeGrinder, ref UnanchorAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnPowerChanged(Entity<SlimeGrinderComponent> grinder, ref PowerChangedEvent args)
    {
        if (args.Powered && grinder.Comp.ProcessingTimer > 0)
            EnsureComp<ActiveSlimeGrinderComponent>(grinder);
        else RemCompDeferred<ActiveSlimeGrinderComponent>(grinder);
    }

    #endregion

    private void OnClimbedOn(Entity<SlimeGrinderComponent> grinder, ref ClimbedOnEvent args)
    {
        TryQueueProcess(args.Climber, grinder);
    }

    private void OnActivate(Entity<SlimeGrinderComponent> grinder, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryStartProcessing(grinder);
    }

    private void OnExamined(Entity<SlimeGrinderComponent> grinder, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(
            "slime-grinder-examined",
            ("amount", grinder.Comp.SlimeContainer.ContainedEntities.Count)));
    }

    private void OnGetVerbs(Entity<SlimeGrinderComponent> grinder, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var canStart = CanStartProcessing(grinder);
        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("slime-grinder-verb-start"),
            Disabled = !canStart,
            Message = Loc.GetString(canStart
                ? "slime-grinder-verb-start-ready"
                : "slime-grinder-verb-start-unavailable"),
            Act = () => TryStartProcessing(grinder),
        });
    }

    private void OnDoAfter(Entity<SlimeGrinderComponent> grinder, ref ReclaimerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Used is not { } toProcess)
            return;

        args.Handled = TryQueueProcess(toProcess, grinder);
    }

    public bool TryQueueProcess(EntityUid toProcess, Entity<SlimeGrinderComponent> grinder)
    {
        if (!CanQueue(grinder, toProcess))
            return false;

        return _container.Insert(toProcess, grinder.Comp.SlimeContainer);
    }

    private bool TryStartProcessing(Entity<SlimeGrinderComponent> grinder)
    {
        if (!CanStartProcessing(grinder))
            return false;

        foreach (var slime in grinder.Comp.SlimeContainer.ContainedEntities.ToArray())
            QueueYield(slime, grinder);

        EnsureComp<ActiveSlimeGrinderComponent>(grinder);
        return true;
    }

    private void QueueYield(
        EntityUid toProcess,
        Entity<SlimeGrinderComponent> grinder,
        PhysicsComponent? physics = null,
        SlimeComponent? slime = null)
    {
        if (!Resolve(toProcess, ref physics, ref slime))
            return;

        grinder.Comp.ProcessingTimer += physics.FixturesMass * grinder.Comp.ProcessingTimePerUnitMass;

        var extractProto = _xenobio.GetProducedExtract((toProcess, slime));
        var extractQuantity = slime.ExtractsProduced;

        if (!grinder.Comp.YieldQueue.ContainsKey(extractProto))
            grinder.Comp.YieldQueue.Add(extractProto, extractQuantity);
        else grinder.Comp.YieldQueue[extractProto] += extractQuantity;

        foreach (var ent in _container.EmptyContainer(slime.Stomach)) // spew everything out jic
        {
            _throwing.TryThrow(ent, _robustRandom.NextVector2() * 5);
        }
        QueueDel(toProcess);
    }

    private bool CanQueue(Entity<SlimeGrinderComponent> grinder, EntityUid slime)
    {
        if (!Transform(grinder).Anchored
        || !HasComp<SlimeComponent>(slime)
        || _container.IsEntityInContainer(slime)
        || (TryComp<MobStateComponent>(slime, out var mobState) && mobState.CurrentState != MobState.Dead))
            return false;

        return true;
    }

    private bool CanStartProcessing(Entity<SlimeGrinderComponent> grinder)
    {
        return Transform(grinder).Anchored &&
               !HasComp<ActiveSlimeGrinderComponent>(grinder) &&
               grinder.Comp.SlimeContainer.ContainedEntities.Count > 0 &&
               (!TryComp<ApcPowerReceiverComponent>(grinder, out var power) || power.Powered);
    }
}
