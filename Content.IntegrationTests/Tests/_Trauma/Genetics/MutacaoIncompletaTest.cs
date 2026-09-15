using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Pair;
using Content.Trauma.Shared.Genetics.Mutations;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma.Genetics;

[TestFixture]
public sealed class MutacaoIncompletaTest
{
    private static readonly EntProtoId<MutationComponent> Incompreensivel = "MutationUnintelligible";
    private static readonly EntProtoId<MutationComponent> Chapado = "MutationStoner";
    private const string Humano = "TestMutacaoIncompletaMob";

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  parent: MobHuman
  id: TestMutacaoIncompletaMob
  components:
  - type: Mutatable
";

    [Test]
    public async Task IncompreensivelNaoApareceNoGenomaDeNinguem()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true });
        var map = await pair.CreateTestMap();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var mutation = entMan.System<MutationSystem>();

        await pair.Server.WaitAssertion(() =>
            Assert.Multiple(() =>
            {
                Assert.That(mutation.UnlockedMutations, Does.Not.Contain(Incompreensivel),
                    "a mutação sem efeito continua liberada para sorteio");
                Assert.That(mutation.NegativeMutations, Does.Not.Contain(Incompreensivel),
                    "a mutação sem efeito continua na lista de instabilidade negativa");
            }));

        var mobs = new List<EntityUid>();
        await pair.Server.WaitPost(() =>
        {
            for (var i = 0; i < 80; i++)
                mobs.Add(entMan.SpawnEntity(Humano, map.GridCoords));
        });
        await pair.Server.WaitRunTicks(1);

        await pair.Server.WaitAssertion(() =>
        {
            var dormentes = mobs
                .Select(m => entMan.GetComponent<MutatableComponent>(m))
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(dormentes.Count(c => !mutation.IsForeign(c, Chapado)), Is.GreaterThan(0),
                    "nem a mutação liberada de controle apareceu, então o sorteio não mede nada");
                Assert.That(dormentes.Count(c => !mutation.IsForeign(c, Incompreensivel)), Is.Zero,
                    "a mutação sem efeito apareceu dormente num genoma");
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ChapadoContinuaCaminhoDaReceita()
    {
        await using var pair = await PoolManager.GetServerClient();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var mutation = entMan.System<MutationSystem>();

        await pair.Server.WaitAssertion(() =>
            Assert.Multiple(() =>
            {
                Assert.That(mutation.UnlockedMutations, Does.Contain(Chapado),
                    "o Chapado saiu do sorteio, e sem ele a receita que o usa não tem de onde vir");
                Assert.That(mutation.GetPossibleRecipes(Chapado), Is.Not.Empty,
                    "o Chapado não é mais ingrediente de receita, e aí não há motivo para ele seguir sorteável");
            }));

        await pair.CleanReturnAsync();
    }
}
