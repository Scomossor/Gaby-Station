using Content.IntegrationTests.Pair;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Clumsy;
using Content.Shared.CombatMode;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Trauma.Actions;

[TestFixture]
public sealed class EffectActionTest
{
    private static readonly EntProtoId NaAcao = "TestEffectActionOnAction";
    private static readonly EntProtoId SemFlag = "TestEffectActionWithoutFlag";
    private static readonly EntProtoId NoAlvo = "TestEffectActionTarget";
    private static readonly EntProtoId NoAlvoEEmQuemUsa = "TestEffectActionTargetOnPerformed";

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  parent: BaseAction
  id: TestEffectActionOnAction
  components:
  - type: Action
    raiseOnAction: true
    useDelay: 10
  - type: InstantAction
    event: !type:EffectInstantActionEvent
  - type: EffectAction
    effects:
    - !type:AddComponents
      components:
      - type: Clumsy

- type: entity
  parent: BaseAction
  id: TestEffectActionWithoutFlag
  components:
  - type: Action
    useDelay: 10
  - type: InstantAction
    event: !type:EffectInstantActionEvent
  - type: EffectAction
    effects:
    - !type:AddComponents
      components:
      - type: Clumsy

- type: entity
  parent: BaseAction
  id: TestEffectActionTarget
  components:
  - type: Action
    raiseOnAction: true
  - type: TargetAction
  - type: EntityTargetAction
    event: !type:EffectTargetActionEvent
  - type: EffectAction
    effects:
    - !type:AddComponents
      components:
      - type: Clumsy

- type: entity
  parent: BaseAction
  id: TestEffectActionTargetOnPerformed
  components:
  - type: Action
    raiseOnAction: true
  - type: TargetAction
  - type: EntityTargetAction
    event: !type:EffectTargetActionEvent
  - type: EffectAction
    onPerformed: true
    effects:
    - !type:AddComponents
      components:
      - type: Clumsy
";

    private sealed record Montagem(TestPair Pair, EntityUid Mob, EntityUid Outro);

    private static async Task<Montagem> Montar()
    {
        var pair = await PoolManager.GetServerClient(new PoolSettings { Dirty = true });
        var entMan = pair.Server.ResolveDependency<IEntityManager>();
        var map = await pair.CreateTestMap();

        EntityUid mob = default, outro = default;
        await pair.Server.WaitPost(() =>
        {
            mob = entMan.SpawnEntity("MobHuman", map.GridCoords);
            outro = entMan.SpawnEntity("MobHuman", map.GridCoords);
        });
        await pair.Server.WaitAssertion(() =>
        {
            Assert.That(entMan.HasComponent<ClumsyComponent>(mob), Is.False, "o mob da montagem ja nasceu desastrado");
            Assert.That(entMan.HasComponent<ClumsyComponent>(outro), Is.False, "o alvo da montagem ja nasceu desastrado");
        });
        return new Montagem(pair, mob, outro);
    }

    private static Entity<ActionComponent> Dar(Montagem m, EntProtoId proto)
    {
        var actions = m.Pair.Server.ResolveDependency<IEntityManager>().System<SharedActionsSystem>();
        var uid = actions.AddAction(m.Mob, proto);
        Assert.That(uid, Is.Not.Null, $"a acao {proto} nao entrou no mob");
        var action = actions.GetAction(uid);
        Assert.That(action, Is.Not.Null);
        return action!.Value;
    }

    [Test]
    public async Task AcaoAntigaContinuaIndoParaQuemUsa()
    {
        var m = await Montar();
        var entMan = m.Pair.Server.ResolveDependency<IEntityManager>();
        var actions = entMan.System<SharedActionsSystem>();

        await m.Pair.Server.WaitAssertion(() =>
        {
            var combate = entMan.GetComponent<CombatModeComponent>(m.Mob);
            var action = actions.GetAction(combate.CombatToggleActionEntity);
            Assert.That(action, Is.Not.Null, "o mob nao tem a acao de modo de combate");
            Assert.That(action!.Value.Comp.RaiseOnAction, Is.False, "a acao de controle devia ser uma acao antiga");

            var antes = combate.IsInCombatMode;
            actions.PerformAction(m.Mob, action.Value);
            Assert.That(combate.IsInCombatMode, Is.Not.EqualTo(antes), "a acao antiga nao chegou em quem usa");
        });

        await m.Pair.CleanReturnAsync();
    }

    [Test]
    public async Task RaiseOnActionLevaOEventoParaAAcao()
    {
        var m = await Montar();
        var entMan = m.Pair.Server.ResolveDependency<IEntityManager>();
        var actions = entMan.System<SharedActionsSystem>();

        await m.Pair.Server.WaitAssertion(() =>
        {
            var action = Dar(m, NaAcao);
            actions.PerformAction(m.Mob, action);

            Assert.That(entMan.HasComponent<ClumsyComponent>(m.Mob), "o evento nao chegou na acao e o efeito nao foi aplicado");
            Assert.That(action.Comp.Cooldown, Is.Not.Null, "a acao nao foi marcada como tratada e a recarga nao comecou");
        });

        await m.Pair.CleanReturnAsync();
    }

    [Test]
    public async Task SemRaiseOnActionOEventoNaoVaiParaAAcao()
    {
        var m = await Montar();
        var entMan = m.Pair.Server.ResolveDependency<IEntityManager>();
        var actions = entMan.System<SharedActionsSystem>();

        await m.Pair.Server.WaitAssertion(() =>
        {
            var action = Dar(m, SemFlag);
            Assert.That(action.Comp.RaiseOnAction, Is.False);
            actions.PerformAction(m.Mob, action);

            Assert.That(entMan.HasComponent<ClumsyComponent>(m.Mob), Is.False, "sem raiseOnAction o evento chegou na acao");
            Assert.That(action.Comp.Cooldown, Is.Null, "sem raiseOnAction alguem tratou o evento");
        });

        await m.Pair.CleanReturnAsync();
    }

    [Test]
    public async Task AcaoDeAlvoAplicaSoNoAlvo()
    {
        var m = await Montar();
        var entMan = m.Pair.Server.ResolveDependency<IEntityManager>();
        var actions = entMan.System<SharedActionsSystem>();

        await m.Pair.Server.WaitAssertion(() =>
        {
            var action = Dar(m, NoAlvo);
            var ev = actions.GetEvent(action) as EntityTargetActionEvent;
            Assert.That(ev, Is.Not.Null, "a acao de alvo nao tem evento de alvo");
            ev!.Target = m.Outro;
            actions.PerformAction(m.Mob, action, ev);

            Assert.That(entMan.HasComponent<ClumsyComponent>(m.Outro), "o efeito nao chegou no alvo");
            Assert.That(entMan.HasComponent<ClumsyComponent>(m.Mob), Is.False, "sem onPerformed o efeito chegou em quem usa");
        });

        await m.Pair.CleanReturnAsync();
    }

    [Test]
    public async Task OnPerformedAplicaTambemEmQuemUsa()
    {
        var m = await Montar();
        var entMan = m.Pair.Server.ResolveDependency<IEntityManager>();
        var actions = entMan.System<SharedActionsSystem>();

        await m.Pair.Server.WaitAssertion(() =>
        {
            var action = Dar(m, NoAlvoEEmQuemUsa);
            var ev = actions.GetEvent(action) as EntityTargetActionEvent;
            Assert.That(ev, Is.Not.Null, "a acao de alvo nao tem evento de alvo");
            ev!.Target = m.Outro;
            actions.PerformAction(m.Mob, action, ev);

            Assert.That(entMan.HasComponent<ClumsyComponent>(m.Outro), "o efeito nao chegou no alvo");
            Assert.That(entMan.HasComponent<ClumsyComponent>(m.Mob), "com onPerformed o efeito nao chegou em quem usa");
        });

        await m.Pair.CleanReturnAsync();
    }
}
