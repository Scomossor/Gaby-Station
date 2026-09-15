using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Pair;
using Content.Server.Polymorph.Components;
using Content.Shared.StatusEffectNew;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma.Genetics;

[TestFixture]
public sealed class InstabilidadeTest
{
    private const string Mob = "TestInstabilidadeMob";
    private const string Derretimento = "DnaMeltingStatusEffect";
    private const int Semente = 20260912;

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  parent: MobHuman
  id: TestInstabilidadeMob
  components:
  - type: Mutatable
    maxDormant: 0
";

    [Test]
    public async Task InstabilidadeNoMaximoPoeETiraOStatus()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true });
        var map = await pair.CreateTestMap();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var mutation = entMan.System<MutationSystem>();
        var status = entMan.System<StatusEffectsSystem>();

        EntityUid mob = default;
        await pair.Server.WaitPost(() => mob = entMan.SpawnEntity(Mob, map.GridCoords));
        await pair.Server.WaitRunTicks(1);

        await pair.Server.WaitAssertion(() =>
        {
            var comp = entMan.GetComponent<MutatableComponent>(mob);
            Assert.That(status.HasStatusEffect(mob, Derretimento), Is.False, "o mob já nasceu derretendo");

            mutation.AddInstability((mob, comp), comp.MaxInstability, automatic: true, predicted: false);
            Assert.That(status.HasStatusEffect(mob, Derretimento), Is.True,
                "chegar no máximo de instabilidade não pôs o status de derretimento");

            mutation.AddInstability((mob, comp), -10, automatic: true, predicted: false);
        });

        await pair.Server.WaitRunTicks(2);

        await pair.Server.WaitAssertion(() =>
            Assert.That(status.HasStatusEffect(mob, Derretimento), Is.False,
                "baixar da instabilidade máxima não tirou o status de derretimento"));

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ConsertarATempoEvitaAConsequencia()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true, ServerSeed = Semente });
        var map = await pair.CreateTestMap();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var mutation = entMan.System<MutationSystem>();
        var status = entMan.System<StatusEffectsSystem>();

        var mobs = new List<EntityUid>();
        await pair.Server.WaitPost(() =>
        {
            for (var i = 0; i < 20; i++)
            {
                mobs.Add(entMan.SpawnEntity(Mob, map.GridCoords));
            }
        });
        await pair.Server.WaitRunTicks(1);

        await pair.Server.WaitPost(() =>
        {
            foreach (var mob in mobs)
            {
                var comp = entMan.GetComponent<MutatableComponent>(mob);
                mutation.AddInstability((mob, comp), comp.MaxInstability, automatic: true, predicted: false);
                mutation.AddInstability((mob, comp), -comp.MaxInstability, automatic: true, predicted: false);
            }
        });
        await pair.Server.WaitRunTicks(2);

        await pair.Server.WaitAssertion(() =>
        {
            foreach (var mob in mobs)
            {
                Assert.That(entMan.EntityExists(mob), Is.True, "o mob consertado a tempo foi apagado");
            }

            Assert.That(Polimorfos(entMan), Is.Empty, "o mob consertado a tempo foi transformado");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task InstabilidadeMantidaTemConsequencia()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true, ServerSeed = Semente });
        var map = await pair.CreateTestMap();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var mutation = entMan.System<MutationSystem>();
        var status = entMan.System<StatusEffectsSystem>();

        var mobs = new List<EntityUid>();
        await pair.Server.WaitPost(() =>
        {
            for (var i = 0; i < 20; i++)
            {
                mobs.Add(entMan.SpawnEntity(Mob, map.GridCoords));
            }
        });
        await pair.Server.WaitRunTicks(1);

        await pair.Server.WaitPost(() =>
        {
            foreach (var mob in mobs)
            {
                var comp = entMan.GetComponent<MutatableComponent>(mob);
                mutation.AddInstability((mob, comp), comp.MaxInstability, automatic: true, predicted: false);
                status.TryRemoveStatusEffect(mob, Derretimento);
            }
        });
        await pair.Server.WaitRunTicks(2);

        await pair.Server.WaitAssertion(() =>
        {
            var sumiram = mobs.Count(m => !entMan.EntityExists(m));
            var transformados = Polimorfos(entMan).Count;

            Assert.That(sumiram + transformados, Is.GreaterThan(0),
                "vinte mobs mantidos na instabilidade máxima e nenhum foi apagado nem transformado, "
                + "então o sorteio de consequências não chegou a rodar");
        });

        await pair.CleanReturnAsync();
    }

    private static List<EntityUid> Polimorfos(IEntityManager entMan)
    {
        var achados = new List<EntityUid>();
        var busca = entMan.EntityQueryEnumerator<PolymorphedEntityComponent>();
        while (busca.MoveNext(out var uid, out _))
        {
            achados.Add(uid);
        }
        return achados;
    }
}
