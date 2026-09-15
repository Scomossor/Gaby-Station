// SPDX-FileCopyrightText: 2026 Project Dumont
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable
using Content.Goobstation.Shared.Xenobiology.Components;
using Content.Goobstation.Maths.FixedPoint;
using Content.IntegrationTests.Pair;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests._Goobstation.Xenobiology;

[TestFixture]
public sealed class SlimeLatchTest
{
    private const string PlasmamanPrototype = "MobPlasmaman";
    private const string SlimePrototype = "MobSlimeXenobioBaby";

    [Test]
    public async Task FeedingOnEmptyBloodstreamDoesNotCrash()
    {
        var pair = await PoolManager.GetServerClient();
        var map = await pair.CreateTestMap();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        EntityUid plasmaman = default;
        EntityUid slime = default;

        await server.WaitPost(() =>
        {
            plasmaman = entMan.SpawnEntity(PlasmamanPrototype, map.GridCoords);
            slime = entMan.SpawnEntity(SlimePrototype, map.GridCoords);
        });

        await pair.RunTicksSync(5);

        await server.WaitPost(() =>
        {
            var stomachs = entMan.System<SharedBodySystem>().GetBodyOrganEntityComps<StomachComponent>(slime);
            Assert.That(stomachs, Is.Not.Empty);

            var bloodstream = entMan.GetComponent<BloodstreamComponent>(plasmaman);
            var solutions = entMan.System<SharedSolutionContainerSystem>();
            Assert.That(
                solutions.ResolveSolution(
                    plasmaman,
                    bloodstream.BloodSolutionName,
                    ref bloodstream.BloodSolution,
                    out _),
                Is.True);
            Assert.That(
                solutions.ResolveSolution(
                    plasmaman,
                    bloodstream.ChemicalSolutionName,
                    ref bloodstream.ChemicalSolution,
                    out _),
                Is.True);

            var bloodSolution = bloodstream.BloodSolution
                ?? throw new InvalidOperationException("Plasmaman blood solution was not resolved.");
            var chemicalSolution = bloodstream.ChemicalSolution
                ?? throw new InvalidOperationException("Plasmaman chemical solution was not resolved.");
            solutions.RemoveAllSolution(bloodSolution);
            solutions.RemoveAllSolution(chemicalSolution);

            var damage = entMan.EnsureComponent<SlimeDamageOvertimeComponent>(plasmaman);
            damage.SourceEntityUid = slime;
            damage.Interval = TimeSpan.Zero;
            damage.NextTickTime = TimeSpan.Zero;
            entMan.Dirty(plasmaman, damage);
        });

        await pair.RunTicksSync(2);

        await server.WaitPost(() =>
        {
            Assert.That(entMan.EntityExists(plasmaman), Is.True);
            Assert.That(entMan.EntityExists(slime), Is.True);

            var damage = entMan.GetComponent<SlimeDamageOvertimeComponent>(plasmaman);
            var bloodstream = entMan.GetComponent<BloodstreamComponent>(plasmaman);
            var solutions = entMan.System<SharedSolutionContainerSystem>();
            Assert.That(
                solutions.ResolveSolution(
                    plasmaman,
                    bloodstream.ChemicalSolutionName,
                    ref bloodstream.ChemicalSolution,
                    out var chemicals),
                Is.True);
            var chemicalSolution = chemicals
                ?? throw new InvalidOperationException("Plasmaman chemical solution was not resolved.");
            Assert.That(
                chemicalSolution.GetTotalPrototypeQuantity(damage.ToxinReagent.Id),
                Is.GreaterThan(FixedPoint2.Zero));
        });

        await pair.CleanReturnAsync();
    }
}
