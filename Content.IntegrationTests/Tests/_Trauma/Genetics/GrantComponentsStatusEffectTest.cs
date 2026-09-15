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
    private const string OutraFonte = "TestGrantClumsyOther";

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  parent: MobStatusEffectBase
  id: TestGrantClumsyOther
  components:
  - type: GrantComponentsStatusEffect
    components:
    - type: Clumsy
";

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

    private static async Task Aplicar(TestPair pair, EntityUid mob, EntProtoId efeito)
    {
        var status = pair.Server.ResolveDependency<IEntityManager>().System<StatusEffectsSystem>();
        await pair.Server.WaitPost(() =>
            Assert.That(status.TryAddStatusEffect(mob, efeito, out _), $"o efeito {efeito} nao entrou no mob"));
        await pair.RunTicksSync(3);
    }

    private static async Task Tirar(TestPair pair, EntityUid mob, EntProtoId efeito)
    {
        var status = pair.Server.ResolveDependency<IEntityManager>().System<StatusEffectsSystem>();
        await pair.Server.WaitPost(() =>
            Assert.That(status.TryRemoveStatusEffect(mob, efeito), $"o efeito {efeito} nao saiu do mob"));
        // a remocao apaga a entidade do efeito no fim do tick
        await pair.RunTicksSync(3);
    }

    private static async Task Desastrado_(TestPair pair, EntityUid mob, bool esperado, string mensagem)
    {
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        await pair.Server.WaitAssertion(() =>
            Assert.That(entMan.HasComponent<ClumsyComponent>(mob), Is.EqualTo(esperado), mensagem));
    }

    [Test]
    public async Task PoeETiraOComponente()
    {
        var (pair, mob) = await Montar(jaDesastrado: false);

        await Aplicar(pair, mob, Desastrado);
        await Desastrado_(pair, mob, true, "o efeito nao deu o componente");

        await Tirar(pair, mob, Desastrado);
        await Desastrado_(pair, mob, false, "o efeito saiu e o componente que ele pos ficou");

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task NaoTiraOQueJaEraDoMob()
    {
        var (pair, mob) = await Montar(jaDesastrado: true);

        await Aplicar(pair, mob, Desastrado);
        await Tirar(pair, mob, Desastrado);
        await Desastrado_(pair, mob, true, "o efeito tirou o desastrado que o mob ja tinha");

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DuasFontesSaiPrimeiroQuemPos()
    {
        var (pair, mob) = await Montar(jaDesastrado: false);

        await Aplicar(pair, mob, Desastrado);
        await Aplicar(pair, mob, OutraFonte);

        await Tirar(pair, mob, Desastrado);
        await Desastrado_(pair, mob, true, "saiu a fonte que pos e o componente sumiu com a outra fonte ainda ativa");

        await Tirar(pair, mob, OutraFonte);
        await Desastrado_(pair, mob, false, "as duas fontes sairam e o componente ficou");

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DuasFontesSaiPrimeiroQuemChegouDepois()
    {
        var (pair, mob) = await Montar(jaDesastrado: false);

        await Aplicar(pair, mob, Desastrado);
        await Aplicar(pair, mob, OutraFonte);

        await Tirar(pair, mob, OutraFonte);
        await Desastrado_(pair, mob, true, "saiu a segunda fonte e o componente sumiu com a primeira ainda ativa");

        await Tirar(pair, mob, Desastrado);
        await Desastrado_(pair, mob, false, "as duas fontes sairam e o componente ficou");

        await pair.CleanReturnAsync();
    }
}
