using System.Linq;
using Content.Server.Medical.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Genetics;
using Content.Shared.Genetics.Systems;
using Content.Shared.Genetics.UI;
using Content.Shared.Stunnable;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.IntegrationTests.Tests._Wega.Genetics;

[TestFixture]
public sealed class GeneticsConsoleAuthorizationTest
{
    private const int Buffer = 1;

    private const int Semente = 20260909;

    [Test]
    public async Task ControlePositivoFabricaUmInjetor()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false, Dirty = true, ServerSeed = Semente });
        var f = await Montar(pair);

        var antes = await ContarInjetores(pair, f.EntMan);

        await Forjar(pair, new DnaModifierConsoleInjectorEvent(f.EntMan.GetNetEntity(f.Console), Buffer));

        Assert.That(await ContarInjetores(pair, f.EntMan), Is.EqualTo(antes + 1),
            "o caminho valido nao fabricou injetor, entao a fixture ainda nao mede autorizacao");

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task NaoFabricaComAInterfaceFechada()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false, Dirty = true, ServerSeed = Semente });

        // energia, scanner e buffer validos: a unica condicao negada e a interface aberta
        var f = await Montar(pair, abrirInterface: false);

        var antes = await ContarInjetores(pair, f.EntMan);

        await Forjar(pair, new DnaModifierConsoleInjectorEvent(f.EntMan.GetNetEntity(f.Console), Buffer));

        Assert.That(await ContarInjetores(pair, f.EntMan), Is.EqualTo(antes),
            "fabricou injetor para quem nunca abriu a interface");

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task NaoFabricaComOJogadorIncapacitado()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false, Dirty = true, ServerSeed = Semente });

        // tudo valido, inclusive a interface aberta: a unica condicao negada e poder agir
        var f = await Montar(pair);

        await pair.Server.WaitPost(() => f.EntMan.EnsureComponent<StunnedComponent>(f.Jogador));
        await pair.RunTicksSync(5);

        var antes = await ContarInjetores(pair, f.EntMan);

        await Forjar(pair, new DnaModifierConsoleInjectorEvent(f.EntMan.GetNetEntity(f.Console), Buffer));

        Assert.That(await ContarInjetores(pair, f.EntMan), Is.EqualTo(antes),
            "fabricou injetor com o jogador atordoado, interface aberta nao basta");

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task NaoFabricaComOConsoleSemEnergia()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false, Dirty = true, ServerSeed = Semente });

        // interface aberta, scanner e buffer validos: a unica condicao negada e a energia
        var f = await Montar(pair, comEnergia: false);

        var antes = await ContarInjetores(pair, f.EntMan);

        await Forjar(pair, new DnaModifierConsoleInjectorEvent(f.EntMan.GetNetEntity(f.Console), Buffer));

        Assert.That(await ContarInjetores(pair, f.EntMan), Is.EqualTo(antes),
            "fabricou injetor com o console sem energia");

        await pair.CleanReturnAsync();
    }

    private sealed record Fixture(IEntityManager EntMan, EntityUid Jogador, EntityUid Console);

    private static async Task<Fixture> Montar(Pair.TestPair pair, bool abrirInterface = true, bool comEnergia = true)
    {
        var server = pair.Server;
        var sEntMan = server.ResolveDependency<IEntityManager>();
        var cEntMan = pair.Client.ResolveDependency<IEntityManager>();
        var session = server.ResolveDependency<IPlayerManager>().Sessions.Single();
        var dnaServer = server.System<DnaServerSystem>();
        var dnaClient = server.System<DnaClientSystem>();
        var power = server.System<PowerReceiverSystem>();
        var ui = server.System<UserInterfaceSystem>();

        var jogador = session.AttachedEntity!.Value;
        EntityUid console = default;
        NetEntity netConsole = default;

        await server.WaitPost(() =>
        {
            sEntMan.RemoveComponent<StunnedComponent>(jogador);

            var coords = sEntMan.GetComponent<TransformComponent>(jogador).Coordinates;
            console = sEntMan.SpawnEntity("DnaModifierConsole", coords);
            netConsole = sEntMan.GetNetEntity(console);

            if (comEnergia)
                power.SetNeedsPower(console, false);

            var scanner = sEntMan.SpawnEntity("MedicalScanner", coords);
            sEntMan.GetComponent<DnaModifierConsoleComponent>(console).GeneticScanner = scanner;
            sEntMan.GetComponent<MedicalScannerComponent>(scanner).ConnectedConsole = console;

            var srv = sEntMan.EnsureComponent<DnaServerComponent>(console);
            var cli = sEntMan.EnsureComponent<DnaClientComponent>(console);
            dnaServer.RegisterClient((console, srv), (console, cli));
            dnaClient.TryAddToBuffer((console, cli), Buffer, new EnzymeInfo());
        });

        // 1) a criacao do console chega ao cliente
        await pair.RunTicksSync(15);

        await pair.Client.WaitAssertion(() =>
        {
            var cConsole = cEntMan.GetEntity(netConsole);
            Assert.That(cEntMan.EntityExists(cConsole), Is.True, "o console nao replicou para o cliente");
            Assert.That(cEntMan.HasComponent<UserInterfaceComponent>(cConsole), Is.True,
                "o UserInterfaceComponent do console ainda nao existe no cliente");
        });

        if (abrirInterface)
            await server.WaitPost(() => ui.OpenUi(console, DnaModifierUiKey.Key, jogador));

        await pair.RunTicksSync(15);

        return new Fixture(sEntMan, jogador, console);
    }

    private static async Task<int> ContarInjetores(Pair.TestPair pair, IEntityManager entMan)
    {
        var total = 0;
        await pair.Server.WaitPost(() =>
        {
            var query = entMan.EntityQueryEnumerator<MetaDataComponent>();
            while (query.MoveNext(out _, out var data))
            {
                if (data.EntityPrototype?.ID == "DnaInjector")
                    total++;
            }
        });
        return total;
    }

    private static async Task Forjar(Pair.TestPair pair, EntityEventArgs ev)
    {
        var net = pair.Client.ResolveDependency<IEntityNetworkManager>();
        await pair.Client.WaitPost(() => net.SendSystemNetworkMessage(ev));
        await pair.RunTicksSync(10);
    }
}
