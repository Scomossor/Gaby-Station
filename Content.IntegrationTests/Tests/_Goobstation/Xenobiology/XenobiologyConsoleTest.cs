// SPDX-FileCopyrightText: 2026 Project Dumont
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Goobstation.Maths.FixedPoint;
using Content.Goobstation.Server.Xenobiology;
using Content.Goobstation.Server.Xenobiology.SlimeGrinder;
using Content.Goobstation.Shared.SurveillanceCamera;
using Content.Goobstation.Shared.Xenobiology.Components;
using Content.IntegrationTests.Pair;
using Content.Server.DeviceLinking.Systems;
using Content.Server.NPC.HTN;
using Content.Server.Power.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Containers;
using Content.Shared.Eye;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.NPC;
using Content.Shared.Power;
using Robust.Shared.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests._Goobstation.Xenobiology;

[TestFixture]
public sealed class XenobiologyConsoleTest
{
    private const string ConsolePrototype = "ComputerScienceXenobiology";
    private const string CameraPrototype = "SurveillanceCameraScienceXenobiology";
    private const string MonkeyCubePrototype = "MonkeyCube";
    private const string MonkeyPrototype = "MobMonkey";
    private const string SlimeGrinderPrototype = "SlimeGrinder";
    private const string SlimePrototype = "MobSlimeXenobioBaby";
    private const string RemotePrototype = "XenobiologyConsoleEye";
    private const string UserPrototype = "MobHuman";

    [Test]
    public async Task ConsoleSessionStartsAndStopsCleanly()
    {
        var pair = await PoolManager.GetServerClient();

        var ctx = await Setup(pair);

        await ctx.Server.WaitPost(() =>
        {
            ActivateConsole(ctx);

            var console = ctx.EntMan.GetComponent<XenobiologyConsoleComponent>(ctx.Console);
            Assert.That(console.ActiveUser, Is.EqualTo(ctx.User));
            Assert.That(console.RemoteEntity, Is.Not.Null);
            Assert.That(ctx.EntMan.EntityExists(console.RemoteEntity!.Value), Is.True);
            Assert.That(ctx.EntMan.HasComponent<XenobiologyConsoleControllerComponent>(ctx.User), Is.True);

            var eye = ctx.EntMan.GetComponent<EyeComponent>(ctx.User);
            Assert.That(eye.Target, Is.EqualTo(console.RemoteEntity));

            var controller = ctx.EntMan.GetComponent<XenobiologyConsoleControllerComponent>(ctx.User);
            PerformAction(ctx, controller.ExitActionEntity);

            Assert.That(console.ActiveUser, Is.Null);
            Assert.That(console.RemoteEntity, Is.Null);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ConsoleRejectsUnauthorizedUser()
    {
        var pair = await PoolManager.GetServerClient();

        var ctx = await Setup(pair, disableAccess: false);

        await ctx.Server.WaitPost(() =>
        {
            ActivateConsole(ctx);

            var console = ctx.EntMan.GetComponent<XenobiologyConsoleComponent>(ctx.Console);
            Assert.That(console.ActiveUser, Is.Null);
            Assert.That(console.RemoteEntity, Is.Null);
            Assert.That(ctx.EntMan.HasComponent<XenobiologyConsoleControllerComponent>(ctx.User), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ConsoleRejectsMonkeyCubeFromUnauthorizedUser()
    {
        var pair = await PoolManager.GetServerClient();

        var ctx = await Setup(pair, disableAccess: false);

        await ctx.Server.WaitPost(() =>
        {
            var cube = ctx.EntMan.SpawnEntity(
                MonkeyCubePrototype,
                ctx.EntMan.GetComponent<TransformComponent>(ctx.Console).Coordinates);
            var interact = new AfterInteractUsingEvent(
                ctx.User,
                cube,
                ctx.Console,
                ctx.EntMan.GetComponent<TransformComponent>(ctx.Console).Coordinates,
                true);

            ctx.EntMan.EventBus.RaiseLocalEvent(ctx.Console, interact);

            var console = ctx.EntMan.GetComponent<XenobiologyConsoleComponent>(ctx.Console);
            Assert.Multiple(() =>
            {
                Assert.That(interact.Handled, Is.True);
                Assert.That(console.MonkeyBiomass, Is.EqualTo(FixedPoint2.Zero));
                Assert.That(ctx.EntMan.EntityExists(cube), Is.True);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ConsoleRejectsCameraWithDifferentTag()
    {
        var pair = await PoolManager.GetServerClient();

        var ctx = await Setup(pair);

        await ctx.Server.WaitPost(() =>
        {
            var camera = ctx.EntMan.GetComponent<ConsoleCameraComponent>(ctx.Camera);
            camera.Tags = ["OtherConsole"];
            ctx.EntMan.Dirty(ctx.Camera, camera);

            ActivateConsole(ctx);

            var console = ctx.EntMan.GetComponent<XenobiologyConsoleComponent>(ctx.Console);
            Assert.That(console.ActiveUser, Is.Null);
            Assert.That(console.RemoteEntity, Is.Null);
            Assert.That(ctx.EntMan.HasComponent<XenobiologyConsoleControllerComponent>(ctx.User), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ConsoleSessionStopsWhenAccessIsRevoked()
    {
        var pair = await PoolManager.GetServerClient();

        var ctx = await Setup(pair);

        await ctx.Server.WaitPost(() =>
        {
            ActivateConsole(ctx);

            var controller = ctx.EntMan.GetComponent<XenobiologyConsoleControllerComponent>(ctx.User);
            controller.NextValidationTime = TimeSpan.Zero;

            var accessReader = ctx.EntMan.GetComponent<AccessReaderComponent>(ctx.Console);
            ctx.EntMan.System<AccessReaderSystem>().SetActive((ctx.Console, accessReader), true);
        });

        await pair.RunTicksSync(2);

        await ctx.Server.WaitPost(() =>
        {
            var console = ctx.EntMan.GetComponent<XenobiologyConsoleComponent>(ctx.Console);
            Assert.That(console.ActiveUser, Is.Null);
            Assert.That(console.RemoteEntity, Is.Null);
            Assert.That(ctx.EntMan.HasComponent<XenobiologyConsoleControllerComponent>(ctx.User), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ConsoleRestoresPreviousEyeAndMovementRelay()
    {
        var pair = await PoolManager.GetServerClient();

        var ctx = await Setup(pair);

        await ctx.Server.WaitPost(() =>
        {
            var coords = ctx.EntMan.GetComponent<TransformComponent>(ctx.Console).Coordinates;
            var previousRemote = ctx.EntMan.SpawnEntity(RemotePrototype, coords);
            var eye = ctx.EntMan.GetComponent<EyeComponent>(ctx.User);

            ctx.EntMan.System<SharedEyeSystem>().SetTarget(ctx.User, previousRemote, eye);
            ctx.EntMan.System<SharedMoverController>().SetRelay(ctx.User, previousRemote);

            ActivateConsole(ctx);

            var controller = ctx.EntMan.GetComponent<XenobiologyConsoleControllerComponent>(ctx.User);
            PerformAction(ctx, controller.ExitActionEntity);

            Assert.That(eye.Target, Is.EqualTo(previousRemote));
            Assert.That(
                ctx.EntMan.GetComponent<RelayInputMoverComponent>(ctx.User).RelayEntity,
                Is.EqualTo(previousRemote));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MonkeyActionsUseConfiguredPrototype()
    {
        var pair = await PoolManager.GetServerClient();

        var ctx = await Setup(pair);

        await ctx.Server.WaitPost(() =>
        {
            InsertMonkeyCube(ctx);

            ActivateConsole(ctx);

            var console = ctx.EntMan.GetComponent<XenobiologyConsoleComponent>(ctx.Console);
            Assert.Multiple(() =>
            {
                Assert.That(console.MonkeyCubeBiomass, Is.EqualTo((FixedPoint2) 1));
                Assert.That(console.MonkeySpawnCost, Is.EqualTo((FixedPoint2) 1));
                Assert.That(console.MonkeyRecycleYield, Is.EqualTo((FixedPoint2) 0.2));
                Assert.That(console.MonkeyBiomass, Is.EqualTo(console.MonkeySpawnCost));
            });

            var controller = ctx.EntMan.GetComponent<XenobiologyConsoleControllerComponent>(ctx.User);
            var existingMonkeys = FindEntitiesByPrototype(ctx, MonkeyPrototype);
            PerformAction(ctx, controller.PlaceMonkeyActionEntity);

            Assert.That(
                console.MonkeyBiomass,
                Is.EqualTo(FixedPoint2.Zero));

            var newMonkeys = FindEntitiesByPrototype(ctx, MonkeyPrototype);
            newMonkeys.ExceptWith(existingMonkeys);
            Assert.That(newMonkeys, Has.Count.EqualTo(1));
            var monkey = newMonkeys.Single();

            var mobState = ctx.EntMan.System<MobStateSystem>();
            mobState.ChangeMobState(monkey, MobState.Dead, ctx.EntMan.GetComponent<MobStateComponent>(monkey));

            PerformAction(ctx, controller.RecycleMonkeyActionEntity);

            Assert.That(console.MonkeyBiomass, Is.EqualTo(console.MonkeyRecycleYield));

            var view = ctx.EntMan.GetComponent<XenobiologyConsoleViewComponent>(ctx.User);
            Assert.That(view.MonkeyBiomass, Is.EqualTo(console.MonkeyRecycleYield));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task RemoteCursorCannotLeaveCameraCoverage()
    {
        var pair = await PoolManager.GetServerClient();

        var ctx = await Setup(pair);

        await ctx.Server.WaitPost(() =>
        {
            ActivateConsole(ctx);

            var console = ctx.EntMan.GetComponent<XenobiologyConsoleComponent>(ctx.Console);
            var remote = console.RemoteEntity!.Value;
            var original = ctx.EntMan.GetComponent<TransformComponent>(remote).Coordinates;

            var xform = ctx.EntMan.System<SharedTransformSystem>();
            xform.SetCoordinates(remote, original.Offset(new Vector2(20f, 0f)));

            var current = ctx.EntMan.GetComponent<TransformComponent>(remote).Coordinates;
            Assert.That(current.TryDistance(ctx.EntMan, original, out var distance), Is.True);
            Assert.That(distance, Is.LessThan(0.01f));
            Assert.That(ctx.EntMan.HasComponent<XenobiologyConsoleControllerComponent>(ctx.User), Is.True);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SlimeActionsStoreReleaseAndScanWithoutTarget()
    {
        var pair = await PoolManager.GetServerClient();

        var ctx = await Setup(pair);
        EntityUid targetSlime = default;
        EntityUid deadSlime = default;

        await ctx.Server.WaitPost(() =>
        {
            ActivateConsole(ctx);

            var console = ctx.EntMan.GetComponent<XenobiologyConsoleComponent>(ctx.Console);
            var remote = console.RemoteEntity!.Value;
            var remoteCoords = ctx.EntMan.GetComponent<TransformComponent>(remote).Coordinates;
            targetSlime = SpawnTestSlime(ctx, remoteCoords.Offset(new Vector2(0.1f, 0f)));
            deadSlime = SpawnTestSlime(ctx, remoteCoords.Offset(new Vector2(0.05f, 0f)));

            var mobState = ctx.EntMan.System<MobStateSystem>();
            mobState.ChangeMobState(deadSlime, MobState.Dead, ctx.EntMan.GetComponent<MobStateComponent>(deadSlime));
        });

        await pair.RunTicksSync(2);

        await ctx.Server.WaitPost(() =>
        {
            var console = ctx.EntMan.GetComponent<XenobiologyConsoleComponent>(ctx.Console);
            var controller = ctx.EntMan.GetComponent<XenobiologyConsoleControllerComponent>(ctx.User);
            var remote = console.RemoteEntity!.Value;
            var remoteCoords = ctx.EntMan.GetComponent<TransformComponent>(remote).Coordinates;
            var xform = ctx.EntMan.System<SharedTransformSystem>();
            xform.SetCoordinates(targetSlime, remoteCoords.Offset(new Vector2(0.1f, 0f)));
            xform.SetCoordinates(deadSlime, remoteCoords.Offset(new Vector2(0.05f, 0f)));

            var targetCoords = ctx.EntMan.GetComponent<TransformComponent>(targetSlime).Coordinates;
            var lookup = ctx.EntMan.System<EntityLookupSystem>();
            var nearbySlimes = lookup.GetEntitiesInRange<SlimeComponent>(remoteCoords, console.SlimeTargetRange, LookupFlags.Dynamic | LookupFlags.Approximate).ToList();

            Assert.That(ctx.EntMan.HasComponent<SlimeComponent>(targetSlime), Is.True);
            Assert.That(remoteCoords.TryDistance(ctx.EntMan, targetCoords, out var targetDistance), Is.True);
            Assert.That(targetDistance, Is.LessThan(console.SlimeTargetRange));
            Assert.That(nearbySlimes.Select(ent => ent.Owner), Does.Contain(targetSlime));

            PerformAction(ctx, controller.GrabSlimeActionEntity);

            Assert.That(console.SlimeContainer.ContainedEntities, Has.Count.EqualTo(1));
            var storedSlime = console.SlimeContainer.ContainedEntities[0];
            Assert.That(storedSlime, Is.EqualTo(deadSlime));
            Assert.That(storedSlime, Is.Not.EqualTo(targetSlime));
            Assert.That(ctx.EntMan.HasComponent<SlimeComponent>(storedSlime), Is.True);
            Assert.That(ctx.EntMan.HasComponent<XenobiologyConsoleStoredSlimeComponent>(storedSlime), Is.True);
            Assert.That(ctx.EntMan.HasComponent<ActiveNPCComponent>(storedSlime), Is.False);

            if (ctx.EntMan.TryGetComponent<HTNComponent>(storedSlime, out var storedHtn))
                Assert.That(storedHtn.Enabled, Is.False);

            var actionBlocker = ctx.EntMan.System<ActionBlockerSystem>();
            Assert.That(actionBlocker.CanMove(storedSlime), Is.False);

            PerformAction(ctx, controller.ReleaseSlimesActionEntity);

            Assert.That(console.SlimeContainer.ContainedEntities, Is.Empty);
            Assert.That(ctx.EntMan.HasComponent<XenobiologyConsoleStoredSlimeComponent>(storedSlime), Is.False);

            if (ctx.EntMan.TryGetComponent<HTNComponent>(storedSlime, out var releasedHtn))
                Assert.That(releasedHtn.Enabled, Is.True);

            PerformAction(ctx, controller.AnalyzeSlimeActionEntity);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SlimeStorageHoldsFiveLivingOrDeadSlimesAndReleasesAll()
    {
        var pair = await PoolManager.GetServerClient();

        var ctx = await Setup(pair);
        var slimes = new List<EntityUid>();

        await ctx.Server.WaitPost(() =>
        {
            ActivateConsole(ctx);

            var console = ctx.EntMan.GetComponent<XenobiologyConsoleComponent>(ctx.Console);
            var remote = console.RemoteEntity!.Value;
            var remoteCoords = ctx.EntMan.GetComponent<TransformComponent>(remote).Coordinates;
            for (var i = 0; i < console.MaxStoredSlimes + 1; i++)
                slimes.Add(SpawnTestSlime(ctx, remoteCoords.Offset(new Vector2(0.05f * (i + 1), 0f))));

            var mobState = ctx.EntMan.System<MobStateSystem>();
            mobState.ChangeMobState(slimes[1], MobState.Dead, ctx.EntMan.GetComponent<MobStateComponent>(slimes[1]));
            mobState.ChangeMobState(slimes[3], MobState.Dead, ctx.EntMan.GetComponent<MobStateComponent>(slimes[3]));
        });

        await pair.RunTicksSync(2);

        await ctx.Server.WaitPost(() =>
        {
            var console = ctx.EntMan.GetComponent<XenobiologyConsoleComponent>(ctx.Console);
            var controller = ctx.EntMan.GetComponent<XenobiologyConsoleControllerComponent>(ctx.User);
            var remote = console.RemoteEntity!.Value;
            var remoteCoords = ctx.EntMan.GetComponent<TransformComponent>(remote).Coordinates;
            var xform = ctx.EntMan.System<SharedTransformSystem>();
            for (var i = 0; i < slimes.Count; i++)
                xform.SetCoordinates(slimes[i], remoteCoords.Offset(new Vector2(0.05f * (i + 1), 0f)));

            var actions = ctx.EntMan.System<SharedActionsSystem>();
            var grabAction = ctx.EntMan.GetComponent<ActionComponent>(controller.GrabSlimeActionEntity!.Value);
            actions.SetUseDelay((controller.GrabSlimeActionEntity.Value, grabAction), null);

            for (var i = 0; i < slimes.Count; i++)
                PerformAction(ctx, controller.GrabSlimeActionEntity);

            Assert.That(console.SlimeContainer.ContainedEntities, Has.Count.EqualTo(console.MaxStoredSlimes));
            Assert.That(slimes.Count(ctx.EntMan.EntityExists), Is.EqualTo(slimes.Count));
            Assert.That(slimes.Count(slime => ctx.EntMan.HasComponent<XenobiologyConsoleStoredSlimeComponent>(slime)),
                Is.EqualTo(console.MaxStoredSlimes));

            PerformAction(ctx, controller.ReleaseSlimesActionEntity);

            Assert.That(console.SlimeContainer.ContainedEntities, Is.Empty);
            Assert.That(slimes.Any(slime => ctx.EntMan.HasComponent<XenobiologyConsoleStoredSlimeComponent>(slime)), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ConsoleSessionStopsWhenUserMovesAway()
    {
        var pair = await PoolManager.GetServerClient();

        var ctx = await Setup(pair);

        await ctx.Server.WaitPost(() =>
        {
            var console = ctx.EntMan.GetComponent<XenobiologyConsoleComponent>(ctx.Console);
            console.SessionValidationInterval = TimeSpan.Zero;

            ActivateConsole(ctx);

            var xform = ctx.EntMan.System<SharedTransformSystem>();
            var consoleCoords = ctx.EntMan.GetComponent<TransformComponent>(ctx.Console).Coordinates;
            xform.SetCoordinates(ctx.User, consoleCoords.Offset(new Vector2(console.UserMaxDistance + 1f, 0f)));
        });

        await pair.RunTicksSync(2);

        await ctx.Server.WaitPost(() =>
        {
            var console = ctx.EntMan.GetComponent<XenobiologyConsoleComponent>(ctx.Console);
            Assert.That(console.ActiveUser, Is.Null);
            Assert.That(console.RemoteEntity, Is.Null);
            Assert.That(ctx.EntMan.HasComponent<XenobiologyConsoleControllerComponent>(ctx.User), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ConsoleSessionStopsWhenPowerDrops()
    {
        var pair = await PoolManager.GetServerClient();

        var ctx = await Setup(pair);

        await ctx.Server.WaitPost(() =>
        {
            ActivateConsole(ctx);

            var powerChanged = new PowerChangedEvent(false, 0f);
            ctx.EntMan.EventBus.RaiseLocalEvent(ctx.Console, ref powerChanged);

            var console = ctx.EntMan.GetComponent<XenobiologyConsoleComponent>(ctx.Console);
            Assert.That(console.ActiveUser, Is.Null);
            Assert.That(console.RemoteEntity, Is.Null);
        });

        await pair.RunTicksSync(2);

        await ctx.Server.WaitPost(() =>
        {
            Assert.That(ctx.EntMan.HasComponent<XenobiologyConsoleControllerComponent>(ctx.User), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ConsoleSessionStopsWhenUserIsPulled()
    {
        var pair = await PoolManager.GetServerClient();

        var ctx = await Setup(pair);

        await ctx.Server.WaitPost(() =>
        {
            ActivateConsole(ctx);

            var puller = ctx.EntMan.SpawnEntity(UserPrototype, ctx.EntMan.GetComponent<TransformComponent>(ctx.Console).Coordinates);
            var pullStarted = new PullStartedMessage(puller, ctx.User);
            ctx.EntMan.EventBus.RaiseLocalEvent(ctx.User, pullStarted);

            var console = ctx.EntMan.GetComponent<XenobiologyConsoleComponent>(ctx.Console);
            Assert.That(console.ActiveUser, Is.Null);
            Assert.That(console.RemoteEntity, Is.Null);
        });

        await pair.RunTicksSync(2);

        await ctx.Server.WaitPost(() =>
        {
            Assert.That(ctx.EntMan.HasComponent<XenobiologyConsoleControllerComponent>(ctx.User), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SlimeGrinderDoesNotCollectNearbyDeadSlime()
    {
        var pair = await PoolManager.GetServerClient();

        var ctx = await Setup(pair);
        EntityUid grinder = default;
        EntityUid slime = default;

        await ctx.Server.WaitPost(() =>
        {
            var coords = ctx.EntMan.GetComponent<TransformComponent>(ctx.Console).Coordinates;
            grinder = SpawnTestGrinder(ctx, coords);
            slime = SpawnTestSlime(ctx, coords.Offset(new Vector2(0.25f, 0f)));

            ctx.EntMan.System<MobStateSystem>()
                .ChangeMobState(slime, MobState.Dead, ctx.EntMan.GetComponent<MobStateComponent>(slime));
        });

        await pair.RunTicksSync(2);

        await ctx.Server.WaitPost(() =>
        {
            var grinderComp = ctx.EntMan.GetComponent<SlimeGrinderComponent>(grinder);
            Assert.That(ctx.EntMan.GetComponent<TransformComponent>(grinder).Anchored, Is.True);
            Assert.That(ctx.EntMan.EntityExists(slime), Is.True);
            Assert.That(grinderComp.SlimeContainer.Contains(slime), Is.False);
            Assert.That(ctx.EntMan.HasComponent<ActiveSlimeGrinderComponent>(grinder), Is.False);
            Assert.That(grinderComp.YieldQueue, Is.Empty);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task LinkedGrinderQueuesDeadSlimeUntilActivated()
    {
        var pair = await PoolManager.GetServerClient();

        var ctx = await Setup(pair);
        EntityUid grinder = default;
        EntityUid slime = default;

        await ctx.Server.WaitPost(() =>
        {
            ActivateConsole(ctx);

            var coords = ctx.EntMan.GetComponent<TransformComponent>(ctx.Console).Coordinates;
            grinder = SpawnTestGrinder(ctx, coords);
            ctx.EntMan.System<DeviceLinkSystem>().LinkDefaults(ctx.User, ctx.Console, grinder);

            var console = ctx.EntMan.GetComponent<XenobiologyConsoleComponent>(ctx.Console);
            var remote = console.RemoteEntity!.Value;
            slime = SpawnTestSlime(
                ctx,
                ctx.EntMan.GetComponent<TransformComponent>(remote).Coordinates.Offset(new Vector2(0.1f, 0f)));
            ctx.EntMan.System<MobStateSystem>()
                .ChangeMobState(slime, MobState.Dead, ctx.EntMan.GetComponent<MobStateComponent>(slime));

            var controller = ctx.EntMan.GetComponent<XenobiologyConsoleControllerComponent>(ctx.User);
            PerformAction(ctx, controller.GrabSlimeActionEntity);

            var grinderComp = ctx.EntMan.GetComponent<SlimeGrinderComponent>(grinder);
            Assert.That(console.SlimeContainer.ContainedEntities, Is.Empty);
            Assert.That(grinderComp.SlimeContainer.Contains(slime), Is.True);
            Assert.That(ctx.EntMan.HasComponent<ActiveSlimeGrinderComponent>(grinder), Is.False);
            Assert.That(grinderComp.YieldQueue, Is.Empty);

            var activate = new ActivateInWorldEvent(ctx.User, grinder, true);
            ctx.EntMan.EventBus.RaiseLocalEvent(grinder, activate);

            Assert.That(activate.Handled, Is.True);
            Assert.That(ctx.EntMan.HasComponent<ActiveSlimeGrinderComponent>(grinder), Is.True);
            Assert.That(grinderComp.YieldQueue.Values.Sum(), Is.GreaterThan(0));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SlimeGrinderRejectsStoredSlime()
    {
        var pair = await PoolManager.GetServerClient();

        var ctx = await Setup(pair);

        await ctx.Server.WaitPost(() =>
        {
            ActivateConsole(ctx);

            var console = ctx.EntMan.GetComponent<XenobiologyConsoleComponent>(ctx.Console);
            var controller = ctx.EntMan.GetComponent<XenobiologyConsoleControllerComponent>(ctx.User);
            var remote = console.RemoteEntity!.Value;
            var remoteCoords = ctx.EntMan.GetComponent<TransformComponent>(remote).Coordinates;
            var slime = SpawnTestSlime(ctx, remoteCoords.Offset(new Vector2(0.1f, 0f)));
            var mobState = ctx.EntMan.System<MobStateSystem>();
            mobState.ChangeMobState(slime, MobState.Dead, ctx.EntMan.GetComponent<MobStateComponent>(slime));

            PerformAction(ctx, controller.GrabSlimeActionEntity);
            Assert.That(console.SlimeContainer.Contains(slime), Is.True);

            var grinder = SpawnTestGrinder(ctx, remoteCoords);
            var grinderComp = ctx.EntMan.GetComponent<SlimeGrinderComponent>(grinder);
            var grinderSystem = ctx.EntMan.System<SlimeGrinderSystem>();

            Assert.That(grinderSystem.TryQueueProcess(slime, (grinder, grinderComp)), Is.False);
            Assert.That(ctx.EntMan.EntityExists(slime), Is.True);
            Assert.That(console.SlimeContainer.Contains(slime), Is.True);
        });

        await pair.CleanReturnAsync();
    }

    private static async Task<TestContext> Setup(TestPair pair, bool disableAccess = true)
    {
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await pair.RunTicksSync(5);

        var entMan = server.ResolveDependency<IEntityManager>();
        TestContext ctx = default;
        await server.WaitPost(() =>
        {
            var player = entMan.SpawnEntity(UserPrototype, map.GridCoords);
            var console = entMan.SpawnEntity(ConsolePrototype, map.GridCoords);
            var camera = entMan.SpawnEntity(CameraPrototype, map.GridCoords);
            if (entMan.TryGetComponent<ApcPowerReceiverComponent>(console, out var powerReceiver))
            {
                powerReceiver.NeedsPower = false;
                powerReceiver.Powered = true;
            }

            if (disableAccess && entMan.TryGetComponent<AccessReaderComponent>(console, out var accessReader))
                entMan.System<AccessReaderSystem>().SetActive((console, accessReader), false);

            if (entMan.TryGetComponent<ApcPowerReceiverComponent>(camera, out var cameraPowerReceiver))
            {
                cameraPowerReceiver.NeedsPower = false;
                cameraPowerReceiver.Powered = true;
            }

            ctx = new TestContext(server, entMan, player, console, camera);
        });

        return ctx;
    }

    private static void ActivateConsole(TestContext ctx)
    {
        var activate = new ActivateInWorldEvent(ctx.User, ctx.Console, true);
        ctx.EntMan.EventBus.RaiseLocalEvent(ctx.Console, activate);
        Assert.That(activate.Handled, Is.True);
    }

    private static void PerformAction(TestContext ctx, EntityUid? action)
    {
        Assert.That(action, Is.Not.Null);
        var actions = ctx.EntMan.System<SharedActionsSystem>();
        var actionComp = ctx.EntMan.GetComponent<ActionComponent>(action!.Value);
        actions.PerformAction(ctx.User, new Entity<ActionComponent>(action.Value, actionComp), predicted: false);
    }

    private static void InsertMonkeyCube(TestContext ctx)
    {
        var cube = ctx.EntMan.SpawnEntity(MonkeyCubePrototype, ctx.EntMan.GetComponent<TransformComponent>(ctx.Console).Coordinates);
        var interact = new AfterInteractUsingEvent(ctx.User, cube, ctx.Console, ctx.EntMan.GetComponent<TransformComponent>(ctx.Console).Coordinates, true);
        ctx.EntMan.EventBus.RaiseLocalEvent(ctx.Console, interact);
        Assert.That(interact.Handled, Is.True);
    }

    private static HashSet<EntityUid> FindEntitiesByPrototype(TestContext ctx, string prototype)
    {
        var entities = new HashSet<EntityUid>();
        var query = ctx.EntMan.EntityQueryEnumerator<MetaDataComponent>();
        while (query.MoveNext(out var uid, out var meta))
        {
            if (meta.EntityPrototype?.ID == prototype)
                entities.Add(uid);
        }

        return entities;
    }

    private static EntityUid SpawnTestGrinder(TestContext ctx, EntityCoordinates coordinates)
    {
        var grinder = ctx.EntMan.SpawnEntity(SlimeGrinderPrototype, coordinates);
        if (ctx.EntMan.TryGetComponent<ApcPowerReceiverComponent>(grinder, out var powerReceiver))
        {
            powerReceiver.NeedsPower = false;
            powerReceiver.Powered = true;
        }

        var xform = ctx.EntMan.System<SharedTransformSystem>();
        var grinderXform = ctx.EntMan.GetComponent<TransformComponent>(grinder);
        if (!grinderXform.Anchored)
            xform.AnchorEntity((grinder, grinderXform));

        return grinder;
    }

    private static EntityUid SpawnTestSlime(TestContext ctx, EntityCoordinates coordinates)
    {
        var slime = ctx.EntMan.SpawnEntity(SlimePrototype, coordinates);
        var slimeComp = ctx.EntMan.EnsureComponent<SlimeComponent>(slime);
        var containers = ctx.EntMan.System<SharedContainerSystem>();
        slimeComp.Stomach = containers.EnsureContainer<Container>(slime, "stomach");
        return slime;
    }

    private readonly record struct TestContext(
        RobustIntegrationTest.ServerIntegrationInstance Server,
        IEntityManager EntMan,
        EntityUid User,
        EntityUid Console,
        EntityUid Camera);
}
