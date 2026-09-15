using System.Linq;
using Content.IntegrationTests.Pair;
using Content.Shared.Bed.Sleep;
using Content.Shared.Flash;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.Trigger.Systems;
using Content.Trauma.Shared.Genetics.Mutations;
using Content.Trauma.Shared.Mobs;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._Trauma.Genetics;

[TestFixture]
public sealed class MutationEffectChainTest
{
    private const string Mob = "TestChainMob";
    private const string Mutation = "TestChainMutation";
    private const string RandomMutation = "TestChainRandomMutation";
    private const string FlashMutation = "TestChainFlashMutation";
    private const string Sono = "StatusEffectForcedSleeping";

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  parent: MobHuman
  id: TestChainMob
  components:
  - type: Mutatable
    maxDormant: 0 # a montagem nao depende do sorteio de dormentes do jogo

- type: entityCondition
  id: TestChainIsAwake
  condition: !type:WhitelistCondition
    whitelist:
      components:
      - AwakeMob

- type: entityEffect
  id: TestChainSeizure
  effects:
  - !type:PopupMessage
    type: Pvs
    visualType: LargeCaution
    messages:
    - entity-effect-popup-seizure
  - !type:Jitter
    amplitude: 20
    time: 29
  - !type:ModifyStatusEffect
    effectProto: StatusEffectForcedSleeping
    time: 20

- type: entity
  abstract: true
  id: TestChainBaseMutation
  components:
  - type: Mutation
    instability: 0
    locked: true
  - type: GenericTriggerCondition
    condition: !type:MutatedNestedCondition
      condition: !type:MobStateCondition
        inverted: true
        mobstate: Dead
  - type: EntityEffectOnTrigger
    effects:
    - !type:RelayMutated
      effect: !type:NestedEffect
        conditions:
        - !type:NestedCondition
          proto: TestChainIsAwake
        - !type:InContainerCondition
          inverted: true
        proto: TestChainSeizure

- type: entity
  parent: TestChainBaseMutation
  id: TestChainMutation

- type: entity
  parent: TestChainBaseMutation
  id: TestChainRandomMutation
  components:
  - type: RandomTrigger
    prob: 1

- type: entity
  parent: TestChainBaseMutation
  id: TestChainFlashMutation
  components:
  - type: TriggerOnFlashed
    prob: 1
";

    private sealed record Montagem(TestPair Pair, EntityUid Mob, EntityUid Mutacao);

    private static async Task<Montagem> Montar(string mutacao)
    {
        var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true });
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var mutation = entMan.System<MutationSystem>();
        var status = entMan.System<StatusEffectsSystem>();
        var container = entMan.System<SharedContainerSystem>();
        var map = await pair.CreateTestMap();

        EntityUid mob = default;
        EntityUid mut = default;
        await server.WaitPost(() =>
        {
            mob = entMan.SpawnEntity(Mob, map.GridCoords);
            Assert.That(mutation.AddMutation(mob, mutacao), "a mutacao de teste nao entrou no mob");
            var comp = entMan.GetComponent<MutatableComponent>(mob);
            Assert.That(mutation.GetMutation((mob, comp), mutacao), Is.Not.Null);
            mut = mutation.GetMutation((mob, comp), mutacao)!.Value.Owner;
        });

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.System<MobStateSystem>().IsAlive(mob), "o mob da montagem nao esta vivo");
            Assert.That(entMan.HasComponent<AwakeMobComponent>(mob), "o mob da montagem nao esta acordado");
            Assert.That(container.IsEntityInContainer(mob), Is.False, "o mob da montagem ja nasceu dentro de container");
            Assert.That(status.HasStatusEffect(mob, Sono), Is.False, "o mob da montagem ja tem o sono forcado");
            Assert.That(entMan.GetComponent<MutationComponent>(mut).Target, Is.EqualTo(mob), "a mutacao nao aponta para o mob");
        });

        return new Montagem(pair, mob, mut);
    }

    private static async Task<bool> Disparar(Montagem m)
    {
        var entMan = m.Pair.Server.ResolveDependency<IEntityManager>();
        var disparou = false;
        await m.Pair.Server.WaitPost(() =>
        {
            disparou = entMan.System<TriggerSystem>().Trigger(m.Mutacao, key: TriggerSystem.DefaultTriggerKey);
        });
        return disparou;
    }

    private static bool TemSono(Montagem m)
    {
        var entMan = m.Pair.Server.ResolveDependency<IEntityManager>();
        return entMan.System<StatusEffectsSystem>().HasStatusEffect(m.Mob, Sono);
    }

    [Test]
    public async Task CadeiaChegaAoMobVivoEAcordado()
    {
        var m = await Montar(Mutation);

        Assert.That(await Disparar(m), "o trigger foi barrado com o mob vivo e acordado");
        await m.Pair.Server.WaitAssertion(() =>
            Assert.That(TemSono(m), "a cadeia inteira rodou e o mob nao recebeu o sono forcado"));

        await m.Pair.CleanReturnAsync();
    }

    [Test]
    public async Task CondicaoDoTriggerBarraMobMorto()
    {
        var m = await Montar(Mutation);
        var entMan = m.Pair.Server.ResolveDependency<IEntityManager>();

        await m.Pair.Server.WaitPost(() =>
            entMan.System<MobStateSystem>().ChangeMobState(m.Mob, MobState.Dead));

        Assert.That(await Disparar(m), Is.False, "a condicao do trigger deixou passar mob morto");
        await m.Pair.Server.WaitAssertion(() =>
            Assert.That(TemSono(m), Is.False, "mob morto recebeu o efeito da mutacao"));

        await m.Pair.CleanReturnAsync();
    }

    [Test]
    public async Task CondicaoAninhadaBarraMobDormindo()
    {
        var m = await Montar(Mutation);
        var entMan = m.Pair.Server.ResolveDependency<IEntityManager>();

        await m.Pair.Server.WaitPost(() =>
            Assert.That(entMan.System<SleepingSystem>().TrySleeping(m.Mob), "o mob nao conseguiu dormir"));
        await m.Pair.Server.WaitAssertion(() =>
            Assert.That(entMan.HasComponent<AwakeMobComponent>(m.Mob), Is.False, "dormindo e ainda marcado como acordado"));

        Assert.That(await Disparar(m), "o trigger foi barrado com o mob vivo");
        await m.Pair.Server.WaitAssertion(() =>
            Assert.That(TemSono(m), Is.False, "a condicao aninhada deixou passar mob dormindo"));

        await m.Pair.CleanReturnAsync();
    }

    [Test]
    public async Task CondicaoInvertidaBarraMobDentroDeContainer()
    {
        var m = await Montar(Mutation);
        var entMan = m.Pair.Server.ResolveDependency<IEntityManager>();
        var containers = entMan.System<SharedContainerSystem>();

        await m.Pair.Server.WaitPost(() =>
        {
            var caixa = entMan.SpawnEntity(null, entMan.GetComponent<TransformComponent>(m.Mob).Coordinates);
            var cont = containers.EnsureContainer<Container>(caixa, "teste");
            Assert.That(containers.Insert(m.Mob, cont), "o mob nao entrou no container");
        });
        await m.Pair.Server.WaitAssertion(() =>
        {
            Assert.That(containers.IsEntityInContainer(m.Mob), "o mob nao esta no container");
            Assert.That(entMan.HasComponent<AwakeMobComponent>(m.Mob), "dentro do container deixou de estar acordado");
        });

        Assert.That(await Disparar(m), "o trigger foi barrado com o mob vivo");
        await m.Pair.Server.WaitAssertion(() =>
            Assert.That(TemSono(m), Is.False, "a condicao invertida deixou passar mob dentro de container"));

        await m.Pair.CleanReturnAsync();
    }

    [Test]
    public async Task TriggerAleatorioDisparaSozinho()
    {
        var m = await Montar(RandomMutation);

        await m.Pair.RunSeconds(3);
        await m.Pair.Server.WaitAssertion(() =>
            Assert.That(TemSono(m), "o trigger aleatorio com probabilidade 1 nao disparou em tres segundos"));

        await m.Pair.CleanReturnAsync();
    }

    [Test]
    public async Task FlashNoMobChegaAoTriggerDaMutacao()
    {
        var m = await Montar(FlashMutation);
        var entMan = m.Pair.Server.ResolveDependency<IEntityManager>();

        await m.Pair.Server.WaitPost(() =>
            entMan.System<SharedFlashSystem>().Flash(m.Mob, null, null, TimeSpan.FromSeconds(1), 0.8f, displayPopup: false));

        await m.Pair.Server.WaitAssertion(() =>
            Assert.That(TemSono(m), "o flash no mob nao chegou ao trigger da mutacao"));

        await m.Pair.CleanReturnAsync();
    }
}
