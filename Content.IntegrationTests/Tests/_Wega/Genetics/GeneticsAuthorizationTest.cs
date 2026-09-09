using System;
using System.Linq;
using Content.Shared.Genetics;
using Content.Shared.Humanoid;
using Content.Server.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Stunnable;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Wega.Genetics;

[TestFixture]
public sealed class GeneticsAuthorizationTest
{
    private const int Semente = 20260909;

    /// <summary>Especie e marcacoes do teste de sexo: as tres sao de peito e Human aceita as tres.</summary>
    private const string Especie = "Human";
    private const string PermitidaA = "ScarChest";
    private const string PermitidaB = "TattooHiveChest";
    private const string ProibidaPorSexo = "ScarTopSurgeryShort";

    private const string CabeloHumano = "HumanHairAfro";
    private const string CabeloDeOutraEspecie = "VoxHairAfro";

    private static readonly Color Alvo = Color.Red;


    [Test]
    public async Task MorfismoNaoFuncionaSemAMutacao()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false, Dirty = true, ServerSeed = Semente });
        var server = pair.Server;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var session = server.ResolveDependency<IPlayerManager>().Sessions.Single();

        Assert.That(session.AttachedEntity, Is.Not.Null);
        var jogador = session.AttachedEntity!.Value;

        Color antes = default;
        await server.WaitPost(() =>
        {
            sEntMan.RemoveComponent<MorphismGenComponent>(jogador);
            sEntMan.RemoveComponent<StunnedComponent>(jogador);
            antes = sEntMan.GetComponent<HumanoidAppearanceComponent>(jogador).SkinColor;
        });

        Assert.That(antes, Is.Not.EqualTo(Alvo), "a cor inicial e igual ao alvo, o teste seria vazio");

        await Forjar(pair, new MorphismChangeBodyColorEvent(
            sEntMan.GetNetEntity(jogador), MorphismChangeBodyColorEvent.BodyPart.Skin, Alvo));

        await server.WaitAssertion(() =>
        {
            var depois = sEntMan.GetComponent<HumanoidAppearanceComponent>(jogador).SkinColor;
            Assert.That(depois, Is.EqualTo(antes), "quem nao tem a mutacao mudou de cor mesmo assim");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MorfismoNaoAlteraOutroJogador()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false, Dirty = true, ServerSeed = Semente });
        var server = pair.Server;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var session = server.ResolveDependency<IPlayerManager>().Sessions.Single();

        var jogador = session.AttachedEntity!.Value;

        EntityUid vitima = default;
        Color antes = default;
        await server.WaitPost(() =>
        {
            sEntMan.EnsureComponent<MorphismGenComponent>(jogador);

            vitima = sEntMan.SpawnEntity("MobHuman", new MapCoordinates(0, 0, sEntMan.GetComponent<TransformComponent>(jogador).MapID));
            antes = sEntMan.GetComponent<HumanoidAppearanceComponent>(vitima).SkinColor;
        });

        await Forjar(pair, new MorphismChangeBodyColorEvent(
            sEntMan.GetNetEntity(vitima), MorphismChangeBodyColorEvent.BodyPart.Skin, Alvo));

        await server.WaitAssertion(() =>
        {
            var depois = sEntMan.GetComponent<HumanoidAppearanceComponent>(vitima).SkinColor;
            Assert.That(depois, Is.EqualTo(antes), "deu para repintar outro jogador pela rede");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MorfismoLegitimoContinuaFuncionando()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false, Dirty = true, ServerSeed = Semente });
        var server = pair.Server;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var session = server.ResolveDependency<IPlayerManager>().Sessions.Single();

        var jogador = session.AttachedEntity!.Value;

        await server.WaitPost(() => sEntMan.EnsureComponent<MorphismGenComponent>(jogador));

        await Forjar(pair, new MorphismChangeBodyColorEvent(
            sEntMan.GetNetEntity(jogador), MorphismChangeBodyColorEvent.BodyPart.Skin, Alvo));

        await server.WaitAssertion(() =>
        {
            var depois = sEntMan.GetComponent<HumanoidAppearanceComponent>(jogador).SkinColor;
            Assert.That(depois, Is.EqualTo(Alvo), "a guarda barrou o uso legitimo da propria mutacao");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MorfismoNaoFuncionaComOJogadorIncapacitado()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false, Dirty = true, ServerSeed = Semente });
        var server = pair.Server;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var session = server.ResolveDependency<IPlayerManager>().Sessions.Single();

        var jogador = session.AttachedEntity!.Value;

        Color antes = default;
        await server.WaitPost(() =>
        {
            sEntMan.EnsureComponent<MorphismGenComponent>(jogador);
            sEntMan.EnsureComponent<StunnedComponent>(jogador);
            antes = sEntMan.GetComponent<HumanoidAppearanceComponent>(jogador).SkinColor;
        });

        Assert.That(antes, Is.Not.EqualTo(Alvo), "a cor inicial e igual ao alvo, o teste seria vazio");

        await Forjar(pair, new MorphismChangeBodyColorEvent(
            sEntMan.GetNetEntity(jogador), MorphismChangeBodyColorEvent.BodyPart.Skin, Alvo));

        await server.WaitAssertion(() =>
        {
            var depois = sEntMan.GetComponent<HumanoidAppearanceComponent>(jogador).SkinColor;
            Assert.That(depois, Is.EqualTo(antes), "atordoado mudou de cor mesmo assim");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MorfismoRemoveMarcacaoNoCaminhoLegitimo()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false, Dirty = true, ServerSeed = Semente });
        var server = pair.Server;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var session = server.ResolveDependency<IPlayerManager>().Sessions.Single();

        var jogador = session.AttachedEntity!.Value;
        var humanoidSys = server.System<HumanoidAppearanceSystem>();
        var markings = server.ResolveDependency<MarkingManager>();

        var antes = 0;
        await server.WaitPost(() =>
        {
            sEntMan.RemoveComponent<StunnedComponent>(jogador);
            sEntMan.EnsureComponent<MorphismGenComponent>(jogador);

            MontarPersonagem(humanoidSys, entMan: sEntMan, alvo: jogador);
            antes = ContarMarcacoes(sEntMan, jogador, MarkingCategories.Hair);
        });

        Assert.That(antes, Is.EqualTo(1), "a fixture nao conseguiu deixar exatamente uma marcacao de cabelo");

        await Forjar(pair, new MorphismSlotEvent(
            sEntMan.GetNetEntity(jogador), MarkingCategories.Hair, MorphismSlotEvent.SlotAction.Remove, 0));

        await server.WaitAssertion(() =>
        {
            Assert.That(ContarMarcacoes(sEntMan, jogador, MarkingCategories.Hair), Is.EqualTo(antes - 1),
                "o caminho valido nao removeu, entao os negativos de slot nao medem nada");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MorfismoRecusaCategoriaInexistente()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false, Dirty = true, ServerSeed = Semente });
        var server = pair.Server;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var session = server.ResolveDependency<IPlayerManager>().Sessions.Single();

        var jogador = session.AttachedEntity!.Value;

        var antes = 0;
        await server.WaitPost(() =>
        {
            sEntMan.RemoveComponent<StunnedComponent>(jogador);
            sEntMan.EnsureComponent<MorphismGenComponent>(jogador);
            antes = ContarMarcacoes(sEntMan, jogador, MarkingCategories.Hair);
        });

        await Forjar(pair, new MorphismSlotEvent(
            sEntMan.GetNetEntity(jogador), (MarkingCategories) 200, MorphismSlotEvent.SlotAction.Remove, 0));

        await server.WaitAssertion(() =>
        {
            Assert.That(ContarMarcacoes(sEntMan, jogador, MarkingCategories.Hair), Is.EqualTo(antes),
                "categoria inexistente passou pela validacao");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MorfismoRecusaSlotForaDaLista()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false, Dirty = true, ServerSeed = Semente });
        var server = pair.Server;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var session = server.ResolveDependency<IPlayerManager>().Sessions.Single();

        var jogador = session.AttachedEntity!.Value;

        var antes = 0;
        await server.WaitPost(() =>
        {
            sEntMan.RemoveComponent<StunnedComponent>(jogador);
            sEntMan.EnsureComponent<MorphismGenComponent>(jogador);
            antes = ContarMarcacoes(sEntMan, jogador, MarkingCategories.Hair);
        });

        await Forjar(pair, new MorphismSlotEvent(
            sEntMan.GetNetEntity(jogador), MarkingCategories.Hair, MorphismSlotEvent.SlotAction.Remove, 99));

        await server.WaitAssertion(() =>
        {
            Assert.That(ContarMarcacoes(sEntMan, jogador, MarkingCategories.Hair), Is.EqualTo(antes),
                "slot fora da lista passou pela validacao");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MorfismoRecusaMarcacaoDeOutroSexo()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false, Dirty = true, ServerSeed = Semente });
        var server = pair.Server;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var session = server.ResolveDependency<IPlayerManager>().Sessions.Single();

        var jogador = session.AttachedEntity!.Value;
        var humanoidSys = server.System<HumanoidAppearanceSystem>();
        var markings = server.ResolveDependency<MarkingManager>();
        var protos = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            sEntMan.RemoveComponent<StunnedComponent>(jogador);
            sEntMan.EnsureComponent<MorphismGenComponent>(jogador);

            humanoidSys.SetSpecies(jogador, Especie);
            humanoidSys.SetSex(jogador, Sex.Female);

            while (ContarMarcacoes(sEntMan, jogador, MarkingCategories.Chest) > 0)
                humanoidSys.RemoveMarking(jogador, MarkingCategories.Chest, 0);

            humanoidSys.AddMarking(jogador, PermitidaA, Color.Black);

            var humanoid = sEntMan.GetComponent<HumanoidAppearanceComponent>(jogador);
            Assert.Multiple(() =>
            {
                Assert.That(humanoid.Species, Is.EqualTo(Especie), "a especie nao ficou como a fixture pediu");
                Assert.That(humanoid.Sex, Is.EqualTo(Sex.Female), "o sexo nao ficou como a fixture pediu");
                Assert.That(ContarMarcacoes(sEntMan, jogador, MarkingCategories.Chest), Is.EqualTo(1),
                    "a fixture nao deixou exatamente uma marcacao de peito");

                Assert.That(markings.CanBeApplied(Especie, Sex.Female, protos.Index<MarkingPrototype>(PermitidaB)),
                    Is.True, PermitidaB + " deveria ser permitida para Human/Female");
                Assert.That(markings.CanBeApplied(Especie, Sex.Male, protos.Index<MarkingPrototype>(ProibidaPorSexo)),
                    Is.True, ProibidaPorSexo + " deveria ser permitida para Human/Male");
                Assert.That(markings.CanBeApplied(Especie, Sex.Female, protos.Index<MarkingPrototype>(ProibidaPorSexo)),
                    Is.False, ProibidaPorSexo + " deveria ser recusada para Human/Female, que e a condicao medida");
            });
        });

        await Forjar(pair, new MorphismChangeMarkingEvent(
            sEntMan.GetNetEntity(jogador), MarkingCategories.Chest, 0, PermitidaB));

        await server.WaitAssertion(() =>
        {
            Assert.That(MarcacaoNoSlot(sEntMan, jogador, MarkingCategories.Chest, 0), Is.EqualTo(PermitidaB),
                "a marcacao permitida nao entrou, entao o negativo abaixo nao mede o sexo");
        });

        await Forjar(pair, new MorphismChangeMarkingEvent(
            sEntMan.GetNetEntity(jogador), MarkingCategories.Chest, 0, ProibidaPorSexo));

        await server.WaitAssertion(() =>
        {
            Assert.That(MarcacaoNoSlot(sEntMan, jogador, MarkingCategories.Chest, 0), Is.EqualTo(PermitidaB),
                "vestiu marcacao restrita ao sexo oposto");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MorfismoRecusaMarcacaoDeOutraEspecie()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false, Dirty = true, ServerSeed = Semente });
        var server = pair.Server;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var session = server.ResolveDependency<IPlayerManager>().Sessions.Single();

        var jogador = session.AttachedEntity!.Value;
        var humanoidSys = server.System<HumanoidAppearanceSystem>();
        var markings = server.ResolveDependency<MarkingManager>();
        var protos = server.ResolveDependency<IPrototypeManager>();

        var antes = string.Empty;
        await server.WaitPost(() =>
        {
            sEntMan.RemoveComponent<StunnedComponent>(jogador);
            sEntMan.EnsureComponent<MorphismGenComponent>(jogador);
            MontarPersonagem(humanoidSys, entMan: sEntMan, alvo: jogador);
            antes = MarcacaoNoSlot(sEntMan, jogador, MarkingCategories.Hair, 0);
        });

        Assert.That(antes, Is.Not.Empty, "a fixture nao deixou marcacao de cabelo para trocar");

        // o par e conhecido: as duas sao de cabelo, e so a especie difere entre elas
        await server.WaitAssertion(() =>
        {
            var humanoid = sEntMan.GetComponent<HumanoidAppearanceComponent>(jogador);
            Assert.Multiple(() =>
            {
                Assert.That(humanoid.Species, Is.EqualTo(Especie), "a especie nao ficou como a fixture pediu");
                Assert.That(markings.CanBeApplied(Especie, humanoid.Sex, protos.Index<MarkingPrototype>(CabeloHumano)),
                    Is.True, CabeloHumano + " deveria ser permitida para " + Especie);
                Assert.That(markings.CanBeApplied(Especie, humanoid.Sex, protos.Index<MarkingPrototype>(CabeloDeOutraEspecie)),
                    Is.False, CabeloDeOutraEspecie + " deveria ser recusada para " + Especie);
            });
        });

        await Forjar(pair, new MorphismChangeMarkingEvent(
            sEntMan.GetNetEntity(jogador), MarkingCategories.Hair, 0, CabeloDeOutraEspecie));

        await server.WaitAssertion(() =>
        {
            Assert.That(MarcacaoNoSlot(sEntMan, jogador, MarkingCategories.Hair, 0), Is.EqualTo(antes),
                "vestiu marcacao proibida para a propria especie");
        });

        await pair.CleanReturnAsync();
    }

    private static string MarcacaoNoSlot(IEntityManager entMan, EntityUid alvo, MarkingCategories categoria, int slot)
    {
        var humanoid = entMan.GetComponent<HumanoidAppearanceComponent>(alvo);
        if (!humanoid.MarkingSet.TryGetCategory(categoria, out var lista) || slot < 0 || slot >= lista.Count)
            return string.Empty;
        return lista[slot].MarkingId;
    }

    private static void MontarPersonagem(HumanoidAppearanceSystem humanoidSys, IEntityManager entMan, EntityUid alvo)
    {
        humanoidSys.SetSpecies(alvo, Especie);
        humanoidSys.SetSex(alvo, Sex.Female);

        while (ContarMarcacoes(entMan, alvo, MarkingCategories.Hair) > 0)
            humanoidSys.RemoveMarking(alvo, MarkingCategories.Hair, 0);

        humanoidSys.AddMarking(alvo, CabeloHumano, Color.Black);
    }

    private static int ContarMarcacoes(IEntityManager entMan, EntityUid alvo, MarkingCategories categoria)
    {
        var humanoid = entMan.GetComponent<HumanoidAppearanceComponent>(alvo);
        return humanoid.MarkingSet.TryGetCategory(categoria, out var lista) ? lista.Count : 0;
    }

    private static async Task Forjar(Pair.TestPair pair, EntityEventArgs ev)
    {
        var net = pair.Client.ResolveDependency<IEntityNetworkManager>();
        await pair.Client.WaitPost(() => net.SendSystemNetworkMessage(ev));
        await pair.RunTicksSync(10);
    }
}
