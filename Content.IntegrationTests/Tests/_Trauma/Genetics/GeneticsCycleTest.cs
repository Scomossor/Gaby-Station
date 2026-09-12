using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Pair;
using Content.Server.Body.Components;
using Content.Server.Humanoid;
using Content.Shared.Clumsy;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Misc;
using Content.Trauma.Shared.EntityEffects;
using Content.Trauma.Shared.Genetics.Mutations;
using Content.Trauma.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma.Genetics;

[TestFixture]
public sealed class GeneticsCycleTest
{
    private const string Mob = "TestCycleMob";
    private const string MobMuitasDormentes = "TestCycleMobManyDormant";
    private const string MobTresDormentes = "TestCycleMobThreeDormant";
    private static readonly EntProtoId<MutationComponent> BracosElasticos = "MutationElasticArms";
    private static readonly EntProtoId<MutationComponent> Acelerado = "MutationStimmed";
    private static readonly ProtoId<EntityEffectPrototype> Felinizar = "MakeFelinid";
    private static readonly ProtoId<EntityEffectPrototype> Desfelinizar = "RevertFelinid";
    private const string Orelha = "FelinidEarsBasic";
    private const string Cauda = "FelinidTailBasic";

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  parent: MobHuman
  id: TestCycleMob
  components:
  - type: Mutatable
    maxDormant: 0

- type: entity
  parent: MobHuman
  id: TestCycleMobManyDormant
  components:
  - type: Mutatable
    maxDormant: 100000 # muito mais que o total de mutacoes liberadas

- type: entity
  parent: MobHuman
  id: TestCycleMobThreeDormant
  components:
  - type: Mutatable
    maxDormant: 3
";

    private static async Task<(TestPair Pair, TestMapData Map)> Par()
    {
        var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true });
        var map = await pair.CreateTestMap();
        return (pair, map);
    }

    private static async Task<EntityUid> Spawn(TestPair pair, EntProtoId proto, TestMapData map, Vector2 desvio = default)
    {
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        EntityUid uid = default;
        await pair.Server.WaitPost(() => uid = entMan.SpawnEntity(proto, map.GridCoords.Offset(desvio)));
        return uid;
    }

    #region Sorteio de dormentes

    [Test]
    public async Task DormentesTerminamSemCandidatosSuficientes()
    {
        var (pair, map) = await Par();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var mutation = entMan.System<MutationSystem>();

        var mob = await Spawn(pair, MobMuitasDormentes, map);
        await pair.Server.WaitAssertion(() =>
        {
            var comp = entMan.GetComponent<MutatableComponent>(mob);
            Assert.That(mutation.UnlockedMutations, Is.Not.Empty, "nenhuma mutacao liberada, o teste nao mede nada");
            Assert.That(comp.Dormant, Is.Unique, "o sorteio repetiu dormente");
            Assert.That(comp.Dormant, Has.Count.EqualTo(mutation.UnlockedMutations.Count),
                "com menos candidatos que a meta, o sorteio devia ficar com todos os candidatos");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DormentesEnchemAteAMeta()
    {
        var (pair, map) = await Par();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();

        var mob = await Spawn(pair, MobTresDormentes, map);
        await pair.Server.WaitAssertion(() =>
        {
            var comp = entMan.GetComponent<MutatableComponent>(mob);
            Assert.That(comp.Dormant, Is.Unique, "o sorteio repetiu dormente");
            Assert.That(comp.Dormant, Has.Count.EqualTo(3), "o sorteio nao encheu ate a meta");
        });

        await pair.CleanReturnAsync();
    }

    #endregion

    #region Felinizado

    private static Dictionary<EntityUid, HashSet<string>> Metabolismo(IEntityManager entMan, EntityUid mob)
    {
        var body = entMan.System<SharedBodySystem>();
        return body.GetBodyOrgans(mob)
            .Where(o => entMan.HasComponent<MetabolizerComponent>(o.Id))
            .ToDictionary(o => o.Id,
                o => entMan.GetComponent<MetabolizerComponent>(o.Id).MetabolizerTypes?.Select(t => t.Id).ToHashSet() ?? new HashSet<string>());
    }

    private static int Marcacoes(TestPair pair, EntityUid mob, string id)
    {
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var protos = pair.Server.ResolveDependency<IPrototypeManager>();
        var humanoid = entMan.GetComponent<HumanoidAppearanceComponent>(mob);
        var categoria = protos.Index<MarkingPrototype>(id).MarkingCategory;
        return humanoid.MarkingSet.TryGetCategory(categoria, out var lista) ? lista.Count(m => m.MarkingId == id) : 0;
    }

    private static async Task Aplicar(TestPair pair, EntityUid mob, ProtoId<EntityEffectPrototype> efeito)
    {
        var effects = pair.Server.ResolveDependency<IEntityManager>().System<TraumaEntityEffectsSystem>();
        await pair.Server.WaitPost(() =>
            Assert.That(effects.TryApplyEffect(mob, efeito), $"as condicoes de {efeito} barraram o mob"));
    }

    private static void IgualAoAntes(Dictionary<EntityUid, HashSet<string>> antes, Dictionary<EntityUid, HashSet<string>> depois)
    {
        Assert.That(depois.Keys, Is.EquivalentTo(antes.Keys), "os orgaos com metabolismo mudaram");
        foreach (var (orgao, tipos) in antes)
        {
            Assert.That(depois[orgao], Is.EquivalentTo(tipos), "a reversao nao devolveu o metabolismo que o orgao tinha");
        }
    }

    [Test]
    public async Task FelinizarEReverterVoltaAoQueEra()
    {
        var (pair, map) = await Par();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var mob = await Spawn(pair, "MobHuman", map);

        Dictionary<EntityUid, HashSet<string>> antes = default!;
        await pair.Server.WaitAssertion(() =>
        {
            antes = Metabolismo(entMan, mob);
            Assert.That(antes, Is.Not.Empty, "o mob nao tem orgao com metabolismo, o teste nao mede nada");
            Assert.That(Marcacoes(pair, mob, Orelha) + Marcacoes(pair, mob, Cauda), Is.Zero, "o mob ja nasceu com orelha ou cauda de gato");
        });

        await Aplicar(pair, mob, Felinizar);
        await pair.Server.WaitAssertion(() =>
        {
            Assert.That(Marcacoes(pair, mob, Orelha), Is.EqualTo(1), "o Felinizado nao pos a orelha");
            Assert.That(Marcacoes(pair, mob, Cauda), Is.EqualTo(1), "o Felinizado nao pos a cauda");
            foreach (var (orgao, tipos) in Metabolismo(entMan, mob))
            {
                Assert.That(tipos, Does.Contain("Animal"), "o Felinizado nao deu metabolismo animal");
                Assert.That(tipos, Does.Not.Contain("Human"), "o Felinizado deixou o metabolismo humano");
            }
        });

        await Aplicar(pair, mob, Desfelinizar);
        await pair.Server.WaitAssertion(() =>
        {
            Assert.That(Marcacoes(pair, mob, Orelha) + Marcacoes(pair, mob, Cauda), Is.Zero, "a reversao deixou orelha ou cauda de gato");
            IgualAoAntes(antes, Metabolismo(entMan, mob));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ReverterPreservaMarcacaoQueJaExistia()
    {
        var (pair, map) = await Par();
        var mob = await Spawn(pair, "MobHuman", map);
        var humanoid = pair.Server.ResolveDependency<IEntityManager>().System<HumanoidAppearanceSystem>();

        await pair.Server.WaitPost(() => humanoid.AddMarking(mob, Orelha, forced: true));
        await pair.Server.WaitAssertion(() =>
            Assert.That(Marcacoes(pair, mob, Orelha), Is.EqualTo(1), "a montagem nao deu a orelha ao personagem"));

        await Aplicar(pair, mob, Felinizar);
        await pair.Server.WaitAssertion(() =>
            Assert.That(Marcacoes(pair, mob, Orelha), Is.EqualTo(1), "o Felinizado duplicou a orelha que o personagem ja tinha"));

        await Aplicar(pair, mob, Desfelinizar);
        await pair.Server.WaitAssertion(() =>
        {
            Assert.That(Marcacoes(pair, mob, Orelha), Is.EqualTo(1), "a reversao tirou a orelha que o personagem ja tinha");
            Assert.That(Marcacoes(pair, mob, Cauda), Is.Zero, "a reversao deixou a cauda que o Felinizado pos");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ReverterPreservaMetabolismoQueNaoEraHumano()
    {
        var (pair, map) = await Par();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var mob = await Spawn(pair, "MobMoth", map);

        Dictionary<EntityUid, HashSet<string>> antes = default!;
        await pair.Server.WaitAssertion(() =>
        {
            antes = Metabolismo(entMan, mob);
            Assert.That(antes.Values.Any(t => !t.Contains("Human")), "a mariposa da montagem nao tem orgao sem metabolismo humano");
        });

        await Aplicar(pair, mob, Felinizar);
        await Aplicar(pair, mob, Desfelinizar);
        await pair.Server.WaitAssertion(() => IgualAoAntes(antes, Metabolismo(entMan, mob)));

        await pair.CleanReturnAsync();
    }

    #endregion

    #region Bracos Elasticos

    private static float Alcance(IEntityManager entMan, EntityUid mob)
        => entMan.TryGetComponent<ExtraReachComponent>(mob, out var comp) ? comp.Bonus : 0f;

    [Test]
    public async Task BracosElasticosDaoAlcanceAoGanharETiramAoPerder()
    {
        var (pair, map) = await Par();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var mutation = entMan.System<MutationSystem>();
        var interaction = entMan.System<SharedInteractionSystem>();

        var mob = await Spawn(pair, Mob, map);
        var alvo = await Spawn(pair, "Crowbar", map, new Vector2(2.6f, 0f));

        await pair.Server.WaitAssertion(() =>
        {
            Assert.That(Alcance(entMan, mob), Is.Zero, "o mob ja nasceu com alcance extra");
            Assert.That(interaction.InRangeUnobstructed(mob, alvo), Is.False, "o alvo da montagem ja esta ao alcance");
        });

        await pair.Server.WaitPost(() => Assert.That(mutation.AddMutation(mob, BracosElasticos), "a mutacao nao entrou"));
        await pair.Server.WaitAssertion(() =>
        {
            Assert.That(Alcance(entMan, mob), Is.EqualTo(1f).Within(0.001f), "os dois bracos nao somaram o alcance no corpo");
            Assert.That(interaction.InRangeUnobstructed(mob, alvo), "com os Bracos Elasticos o alvo continua fora de alcance");
        });

        await pair.Server.WaitPost(() => Assert.That(mutation.RemoveMutation(mob, BracosElasticos), "a mutacao nao saiu"));
        await pair.Server.WaitAssertion(() =>
        {
            Assert.That(Alcance(entMan, mob), Is.Zero, "perder a mutacao nao tirou o alcance do corpo");
            Assert.That(interaction.InRangeUnobstructed(mob, alvo), Is.False, "sem a mutacao o alvo continua ao alcance");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ReencaixarBracoNaoContaDuasVezes()
    {
        var (pair, map) = await Par();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var mutation = entMan.System<MutationSystem>();
        var body = entMan.System<SharedBodySystem>();
        var mob = await Spawn(pair, Mob, map);

        await pair.Server.WaitPost(() => Assert.That(mutation.AddMutation(mob, BracosElasticos), "a mutacao nao entrou"));
        await pair.Server.WaitAssertion(() =>
        {
            var bracos = body.GetBodyChildrenOfType(mob, BodyPartType.Arm).Select(b =>
                entMan.TryGetComponent<ExtraReachComponent>(b.Id, out var r) ? $"{b.Id} bonus {r.Bonus} no corpo {r.AppliedTo}" : $"{b.Id} sem alcance");
            Assert.That(Alcance(entMan, mob), Is.EqualTo(1f).Within(0.001f),
                $"a mutacao nao deu o alcance dos dois bracos: {string.Join("; ", bracos)}");
        });

        EntityUid braco = default;
        (EntityUid Parent, string Slot) encaixe = default;
        await pair.Server.WaitPost(() =>
        {
            braco = body.GetBodyChildrenOfType(mob, BodyPartType.Arm).First().Id;
            encaixe = body.GetParentPartAndSlotOrNull(braco)!.Value;
            Assert.That(body.DetachPart(encaixe.Parent, encaixe.Slot, braco), "o braco nao soltou");
        });
        await pair.Server.WaitAssertion(() =>
            Assert.That(Alcance(entMan, mob), Is.EqualTo(0.5f).Within(0.001f), "soltar o braco nao tirou o bonus dele"));

        await pair.Server.WaitPost(() => Assert.That(body.AttachPart(encaixe.Parent, encaixe.Slot, braco), "o braco nao encaixou"));
        await pair.Server.WaitAssertion(() =>
            Assert.That(Alcance(entMan, mob), Is.EqualTo(1f).Within(0.001f), "reencaixar o braco contou o bonus duas vezes ou nenhuma"));

        await pair.CleanReturnAsync();
    }

    #endregion

    #region Mutacao de efeito de status

    private static readonly EntProtoId<MutationComponent> Desajeitado = "MutationClumsiness";

    [Test]
    public async Task DesastradoSoEnquantoMutado()
    {
        var (pair, map) = await Par();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var mutation = entMan.System<MutationSystem>();
        var mob = await Spawn(pair, Mob, map);

        await pair.Server.WaitAssertion(() =>
            Assert.That(entMan.HasComponent<ClumsyComponent>(mob), Is.False, "o mob da montagem ja nasceu desastrado"));

        await pair.Server.WaitPost(() => Assert.That(mutation.AddMutation(mob, Desajeitado), "a mutacao nao entrou"));
        await pair.RunTicksSync(3);
        await pair.Server.WaitAssertion(() =>
            Assert.That(entMan.HasComponent<ClumsyComponent>(mob), "a mutacao Desastrado nao deixou o mob desastrado"));

        await pair.Server.WaitPost(() => Assert.That(mutation.RemoveMutation(mob, Desajeitado), "a mutacao nao saiu"));
        // o efeito de status sai no fim do tick
        await pair.RunTicksSync(3);
        await pair.Server.WaitAssertion(() =>
            Assert.That(entMan.HasComponent<ClumsyComponent>(mob), Is.False, "perder a mutacao nao tirou o desastrado"));

        await pair.CleanReturnAsync();
    }

    #endregion

    #region Telecinese

    private static async Task<(EntityUid Mob, EntityUid Item, TetherGunComponent Gun)> Amarrado(TestPair pair, TestMapData map)
    {
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var tether = entMan.System<SharedTetherGunSystem>();
        var mob = await Spawn(pair, "MobHuman", map);
        var item = await Spawn(pair, "Crowbar", map, new Vector2(1f, 0f));

        TetherGunComponent gun = default!;
        await pair.Server.WaitPost(() =>
        {
            entMan.EnsureComponent<TelekinesisComponent>(mob);
            gun = entMan.EnsureComponent<TetherGunComponent>(mob);
            Assert.That(tether.TryTether(mob, item, mob, gun), "a telecinese nao amarrou o item");
        });
        await pair.Server.WaitAssertion(() => Assert.That(gun.Tethered, Is.EqualTo(item), "a montagem nao deixou o item amarrado"));
        return (mob, item, gun);
    }

    [Test]
    public async Task AmarraSoltaQuandoOMobMorre()
    {
        var (pair, map) = await Par();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var (mob, _, gun) = await Amarrado(pair, map);

        await pair.Server.WaitPost(() => entMan.System<MobStateSystem>().ChangeMobState(mob, MobState.Dead));
        await pair.Server.WaitAssertion(() => Assert.That(gun.Tethered, Is.Null, "a amarra continuou com o mob morto"));

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AmarraContinuaComOMobVivo()
    {
        var (pair, map) = await Par();
        var (_, item, gun) = await Amarrado(pair, map);

        await pair.RunTicksSync(10);
        await pair.Server.WaitAssertion(() => Assert.That(gun.Tethered, Is.EqualTo(item), "a amarra soltou sozinha com o mob vivo"));

        await pair.CleanReturnAsync();
    }

    #endregion

    [Test]
    public async Task MetabolismoMudaOMultiplicadorDosOrgaos()
    {
        var (pair, map) = await Par();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var mutation = entMan.System<MutationSystem>();
        var corpo = entMan.System<SharedBodySystem>();

        var mob = await Spawn(pair, Mob, map);

        List<float> antes = new();
        await pair.Server.WaitAssertion(() =>
        {
            antes = corpo.GetBodyOrganEntityComps<MetabolizerComponent>(mob)
                .Select(o => o.Comp1.UpdateIntervalMultiplier).ToList();
            Assert.That(antes, Is.Not.Empty, "o mob da montagem não tem órgão que metaboliza");
        });

        await pair.Server.WaitPost(() => Assert.That(mutation.AddMutation(mob, Acelerado), "a mutação não entrou"));
        await pair.Server.WaitAssertion(() =>
        {
            var depois = corpo.GetBodyOrganEntityComps<MetabolizerComponent>(mob)
                .Select(o => o.Comp1.UpdateIntervalMultiplier).ToList();
            Assert.That(depois, Has.Count.EqualTo(antes.Count), "o número de órgãos mudou, então a comparação não vale");
            for (var i = 0; i < depois.Count; i++)
            {
                Assert.That(depois[i], Is.GreaterThan(antes[i]), "ganhar a mutação não mexeu no multiplicador do órgão");
            }
        });

        await pair.Server.WaitPost(() => Assert.That(mutation.RemoveMutation(mob, Acelerado), "a mutação não saiu"));
        await pair.Server.WaitAssertion(() =>
        {
            var voltou = corpo.GetBodyOrganEntityComps<MetabolizerComponent>(mob)
                .Select(o => o.Comp1.UpdateIntervalMultiplier).ToList();
            for (var i = 0; i < voltou.Count; i++)
            {
                Assert.That(voltou[i], Is.EqualTo(antes[i]).Within(0.001f), "perder a mutação não devolveu o multiplicador");
            }
        });

        await pair.CleanReturnAsync();
    }

}
