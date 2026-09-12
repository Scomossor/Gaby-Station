using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Pair;
using Content.Server.Cloning.Components;
using Content.Server.Medical;
using Content.Server.Medical.Components;
using Content.Shared.Research.Components;
using Content.Server.Research.Systems;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Power.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Materials;
using Content.Trauma.Shared.Genetics.Mutations;
using Content.Trauma.Shared.Genetics.Tools;
using Content.Trauma.Shared.Genetics.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests._Trauma.Genetics;

[TestFixture]
public sealed class GeneticsConsoleTest
{
    private const string Console = "ComputerGeneticsConsole";
    private const string Scanner = "MedicalScanner";
    private const string ConsoleClonagem = "ComputerCloningConsole";
    private const string Servidor = "ResearchAndDevelopmentServer";
    private const string Paciente = "MobHuman";
    private const string PacienteMutavel = "TestConsoleMob";
    private const string Disco = "GeneticsDiskUnstableDna";
    private static readonly EntProtoId<MutationComponent> Mutacao = "MutationUnstableDna";

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  parent: MobHuman
  id: TestConsoleMob
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

    private static async Task Ligar(TestPair pair, EntityUid console, EntityUid scanner)
    {
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var links = entMan.System<DeviceLinkSystem>();
        await pair.Server.WaitPost(() => links.SaveLinks(null, console, scanner,
            new List<(string, string)> { (CloningConsoleComponent.ScannerPort, MedicalScannerComponent.ScannerPort) }));
    }

    private static EntityUid? ScannerDo(IEntityManager entMan, EntityUid console)
        => entMan.GetComponent<GeneticsScannerComponent>(console).Scanner;

    private static EntityUid? PacienteDo(IEntityManager entMan, EntityUid console)
        => entMan.GetComponent<GeneticsScannerComponent>(console).ScannedMob;

    [Test]
    public async Task LigarOScannerChegaAoConsole()
    {
        var (pair, map) = await Par();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var console = await Spawn(pair, Console, map);
        var scanner = await Spawn(pair, Scanner, map, new Vector2(1f, 0f));

        await pair.Server.WaitAssertion(() =>
            Assert.That(ScannerDo(entMan, console), Is.Null, "o console nasceu com scanner sem ninguém ligar"));

        await Ligar(pair, console, scanner);

        await pair.Server.WaitAssertion(() =>
            Assert.That(ScannerDo(entMan, console), Is.EqualTo(scanner), "ligar a porta não chegou ao console"));

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task PacienteNoScannerViraOAlvoDoConsole()
    {
        var (pair, map) = await Par();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var console = await Spawn(pair, Console, map);
        var scanner = await Spawn(pair, Scanner, map, new Vector2(1f, 0f));
        var mob = await Spawn(pair, Paciente, map, new Vector2(1f, 0f));
        await Ligar(pair, console, scanner);

        await pair.Server.WaitPost(() =>
            entMan.System<MedicalScannerSystem>().InsertBody(scanner, mob, null));
        await pair.Server.WaitRunTicks(1);

        await pair.Server.WaitAssertion(() =>
            Assert.That(PacienteDo(entMan, console), Is.EqualTo(mob), "o console não enxergou quem entrou no scanner"));

        await pair.Server.WaitPost(() =>
            entMan.System<MedicalScannerSystem>().EjectBody(scanner, null));
        await pair.Server.WaitRunTicks(1);

        await pair.Server.WaitAssertion(() =>
            Assert.That(PacienteDo(entMan, console), Is.Null, "o console continuou com o paciente depois de ele sair"));

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DesancorarEAncorarPerdeERecuperaOScanner()
    {
        var (pair, map) = await Par();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var transform = entMan.System<SharedTransformSystem>();
        var console = await Spawn(pair, Console, map);
        var scanner = await Spawn(pair, Scanner, map);
        await Ligar(pair, console, scanner);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.That(entMan.GetComponent<TransformComponent>(scanner).Anchored, Is.True,
                "o scanner nasceu solto, e sem estar ancorado o teste não mede nada");
            Assert.That(ScannerDo(entMan, console), Is.EqualTo(scanner), "a montagem já começou errada");
        });

        await pair.Server.WaitPost(() => transform.Unanchor(scanner));
        await pair.Server.WaitRunTicks(1);

        await pair.Server.WaitAssertion(() =>
            Assert.That(ScannerDo(entMan, console), Is.Null, "desancorar o scanner não desligou o console"));

        bool ancorou = false;
        await pair.Server.WaitPost(() =>
        {
            transform.SetCoordinates(scanner, map.GridCoords);
            ancorou = transform.AnchorEntity(scanner);
        });
        await pair.Server.WaitRunTicks(1);

        await pair.Server.WaitAssertion(() =>
        {
            Assert.That(ancorou, Is.True, "ancorar de volta falhou, então o teste não mede a volta");
            Assert.That(entMan.GetComponent<TransformComponent>(scanner).Anchored, Is.True, "o scanner não voltou a ficar ancorado");
            Assert.That(ScannerDo(entMan, console), Is.EqualTo(scanner), "ancorar de novo não devolveu o scanner");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ClonagemContinuaEnxergandoOScanner()
    {
        var (pair, map) = await Par();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var console = await Spawn(pair, ConsoleClonagem, map);
        var scanner = await Spawn(pair, Scanner, map, new Vector2(1f, 0f));
        await Ligar(pair, console, scanner);

        await pair.Server.WaitAssertion(() =>
            Assert.That(entMan.GetComponent<CloningConsoleComponent>(console).GeneticScanner, Is.EqualTo(scanner),
                "o console de clonagem parou de achar o scanner depois da mudança"));

        await pair.CleanReturnAsync();
    }

    private static async Task<(EntityUid Console, EntityUid Scanner, EntityUid Mob)> Bancada(TestPair pair, TestMapData map)
    {
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var console = await Spawn(pair, Console, map);
        var scanner = await Spawn(pair, Scanner, map, new Vector2(1f, 0f));
        var mob = await Spawn(pair, PacienteMutavel, map, new Vector2(1f, 0f));
        await Ligar(pair, console, scanner);

        await pair.Server.WaitPost(() =>
        {
            entMan.GetComponent<ApcPowerReceiverComponent>(console).NeedsPower = false;
            entMan.System<MedicalScannerSystem>().InsertBody(scanner, mob, null);
        });
        await pair.Server.WaitRunTicks(1);
        return (console, scanner, mob);
    }

    [Test]
    public async Task ImprimirCriaOInjetorComAMutacaoDoDisco()
    {
        var (pair, map) = await Par();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var (console, _, _) = await Bancada(pair, map);
        var disco = await Spawn(pair, Disco, map);
        var usuario = await Spawn(pair, Paciente, map);
        var material = entMan.System<SharedMaterialStorageSystem>();

        await pair.Server.WaitPost(() =>
        {
            entMan.System<ItemSlotsSystem>().TryInsert(console, "genetics_disk", disco, null);
        });
        await pair.Server.WaitRunTicks(1);

        int antes = 0;
        await pair.Server.WaitAssertion(() =>
        {
            var noConsole = entMan.System<GeneticsDiskSystem>().GetDisk(console);
            Assert.That(noConsole, Is.Not.Null, "o disco não entrou no console, e sem ele a impressão não roda");
            Assert.That(noConsole!.Value.Comp.Mutation, Is.Not.Null, "o disco entrou sem mutação gravada");

            Assert.That(entMan.GetComponent<GeneticsConsoleComponent>(console).Prints, Is.Not.Empty,
                "o console não tem nada para imprimir na lista");
            antes = material.GetMaterialAmount(console, entMan.GetComponent<GeneticsConsoleComponent>(console).Biomass);
            Assert.That(antes, Is.GreaterThan(0), "o console nasceu sem biomassa, e o teste não mede nada assim");
            Assert.That(entMan.EntityQuery<MutatorComponent>(true).Count(), Is.Zero, "já existia injetor antes de imprimir");
        });

        await Imprimir(pair, console, usuario);

        await pair.Server.WaitAssertion(() =>
        {
            var depoisBiomassa = material.GetMaterialAmount(console, entMan.GetComponent<GeneticsConsoleComponent>(console).Biomass);
            Assert.That(depoisBiomassa, Is.LessThan(antes),
                $"a impressão nem chegou a cobrar biomassa: continua {depoisBiomassa} de {antes}");

            var injetores = entMan.EntityQuery<MutatorComponent>(true).ToList();
            Assert.That(injetores, Has.Count.EqualTo(1), "imprimir cobrou biomassa e não criou o injetor");
            Assert.That(injetores[0].Mutations, Does.Contain(Mutacao),
                "o injetor saiu sem a mutação que estava gravada no disco");
            Assert.That(material.GetMaterialAmount(console, entMan.GetComponent<GeneticsConsoleComponent>(console).Biomass),
                Is.LessThan(antes), "imprimir não gastou biomassa");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SemBiomassaNaoSaiInjetor()
    {
        var (pair, map) = await Par();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var (console, _, _) = await Bancada(pair, map);
        var disco = await Spawn(pair, Disco, map);
        var usuario = await Spawn(pair, Paciente, map);
        var material = entMan.System<SharedMaterialStorageSystem>();

        await pair.Server.WaitPost(() =>
        {
            entMan.System<ItemSlotsSystem>().TryInsert(console, "genetics_disk", disco, null);
            var biomassa = entMan.GetComponent<GeneticsConsoleComponent>(console).Biomass;
            material.TryChangeMaterialAmount(console, biomassa, -material.GetMaterialAmount(console, biomassa));
        });
        await pair.Server.WaitRunTicks(1);

        await pair.Server.WaitAssertion(() =>
            Assert.That(material.GetMaterialAmount(console, entMan.GetComponent<GeneticsConsoleComponent>(console).Biomass),
                Is.Zero, "o console não ficou sem biomassa, então o teste não mede o caso sem saldo"));

        await Imprimir(pair, console, usuario);

        await pair.Server.WaitAssertion(() =>
            Assert.That(entMan.EntityQuery<MutatorComponent>(true).Count(), Is.Zero,
                "saiu injetor mesmo sem biomassa para pagar"));

        await pair.CleanReturnAsync();
    }

    private static async Task Imprimir(TestPair pair, EntityUid console, EntityUid usuario)
    {
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var tempo = pair.Server.ResolveDependency<IGameTiming>();

        while (tempo.CurTime < entMan.GetComponent<GeneticsConsoleComponent>(console).NextPrint)
        {
            await pair.Server.WaitRunTicks(30);
        }

        await pair.Server.WaitPost(() =>
        {
            var msg = new GeneticsConsolePrintMessage(0) { UiKey = GeneticsConsoleUiKey.Key, Actor = usuario };
            entMan.EventBus.RaiseLocalEvent(console, msg);
        });
        await pair.Server.WaitRunTicks(2);
    }

    [Test]
    public async Task SequenciarRendePontoUmaVezSo()
    {
        var (pair, map) = await Par();
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var (console, _, mob) = await Bancada(pair, map);
        var servidor = await Spawn(pair, Servidor, map, new Vector2(2f, 0f));
        var pesquisa = entMan.System<ResearchSystem>();
        var genoma = entMan.System<ScannedGenomeSystem>();
        var mutacoes = entMan.System<MutationSystem>();
        var consoleSis = entMan.System<GeneticsConsoleSystem>();

        await pair.Server.WaitPost(() =>
        {
            pesquisa.RegisterClient(console, servidor);
            genoma.ScanGenome(mob);
        });
        await pair.Server.WaitRunTicks(1);

        var pontos = 0;
        var rendeu = false;
        await pair.Server.WaitAssertion(() =>
        {
            var sequencia = genoma.GetSequence(mob, 0);
            Assert.That(sequencia, Is.Not.Null, "o paciente escaneado ficou sem sequência nenhuma");
            var dados = mutacoes.GetRoundData(sequencia!.Mutation);
            Assert.That(dados, Is.Not.Null, "a mutação da sequência não tem dado de rodada");

            sequencia.Bases = dados!.Bases;

            var servidorComp = entMan.GetComponent<ResearchServerComponent>(servidor);
            pontos = servidorComp.Points;
            rendeu = consoleSis.SequenceMutation((console, entMan.GetComponent<GeneticsConsoleComponent>(console)), mob, 0);

            Assert.That(rendeu, Is.True, "sequenciar com as bases certas não funcionou");
            Assert.That(servidorComp.Points, Is.GreaterThan(pontos),
                "sequenciar pela primeira vez não rendeu ponto de pesquisa");

            var depois = servidorComp.Points;
            Assert.That(consoleSis.SequenceMutation((console, entMan.GetComponent<GeneticsConsoleComponent>(console)), mob, 0),
                Is.False, "deu para sequenciar a mesma mutação de novo");
            Assert.That(servidorComp.Points, Is.EqualTo(depois),
                "sequenciar de novo a mesma mutação rendeu ponto outra vez");
        });

        await pair.CleanReturnAsync();
    }

}
