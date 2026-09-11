using Content.IntegrationTests.Pair;
using Content.Shared.Clumsy;
using Content.Shared.StatusEffectNew;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma.Genetics;

[TestFixture]
public sealed class GrantComponentsStatusEffectTest
{
    private static readonly EntProtoId Desastrado = "StatusEffectClumsyMutation";

    private static async Task<(TestPair Pair, EntityUid Mob)> Montar(bool jaDesastrado)
    {
        var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true });
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var map = await pair.CreateTestMap();

        EntityUid mob = default;
        await pair.Server.WaitPost(() =>
        {
            mob = entMan.SpawnEntity("MobHuman", map.GridCoords);
            if (jaDesastrado)
                entMan.AddComponent<ClumsyComponent>(mob);
        });
        await pair.Server.WaitAssertion(() =>
            Assert.That(entMan.HasComponent<ClumsyComponent>(mob), Is.EqualTo(jaDesastrado), "a montagem nao deixou o mob como pedido"));
        return (pair, mob);
    }

    private static async Task Aplicar(TestPair pair, EntityUid mob)
    {
        var status = pair.Server.ResolveDependency<IEntityManager>().System<StatusEffectsSystem>();
        await pair.Server.WaitPost(() =>
            Assert.That(status.TryAddStatusEffect(mob, Desastrado, out _), "o efeito nao entrou no mob"));
        await pair.RunTicksSync(3);
    }

    private static async Task Tirar(TestPair pair, EntityUid mob)
    {
        var status = pair.Server.ResolveDependency<IEntityManager>().System<StatusEffectsSystem>();
        await pair.Server.WaitPost(() =>
            Assert.That(status.TryRemoveStatusEffect(mob, Desastrado), "o efeito nao saiu do mob"));
        // a remocao apaga a entidade do efeito no fim do tick
        await pair.RunTicksSync(3);
    }

    [Test]
    public async Task PoeETiraOComponente()
    {
        var (pair, mob) = await Montar(jaDesastrado: false);
        var entMan = pair.Server.ResolveDependency<IEntityManager>();

        await Aplicar(pair, mob);
        await pair.Server.WaitAssertion(() =>
            Assert.That(entMan.HasComponent<ClumsyComponent>(mob), "o efeito nao deu o componente"));

        await Tirar(pair, mob);
        await pair.Server.WaitAssertion(() =>
            Assert.That(entMan.HasComponent<ClumsyComponent>(mob), Is.False, "o efeito saiu e o componente que ele pos ficou"));

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task NaoTiraOQueJaEraDoMob()
    {
        var (pair, mob) = await Montar(jaDesastrado: true);
        var entMan = pair.Server.ResolveDependency<IEntityManager>();

        await Aplicar(pair, mob);
        await Tirar(pair, mob);
        await pair.Server.WaitAssertion(() =>
            Assert.That(entMan.HasComponent<ClumsyComponent>(mob), "o efeito tirou o desastrado que o mob ja tinha"));

        await pair.CleanReturnAsync();
    }
}
