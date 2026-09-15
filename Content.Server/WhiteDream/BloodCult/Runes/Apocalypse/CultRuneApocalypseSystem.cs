// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
using Content.Server.Emp;
using Content.Server.GameTicking;
using Content.Server.WhiteDream.BloodCult.Gamerule;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.WhiteDream.BloodCult.Runes;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.WhiteDream.BloodCult.Runes.Apocalypse;

public sealed partial class CultRuneApocalypseSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private BloodCultRuleSystem _cultRule = default!; // Dumont
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private CultRuneBaseSystem _runeBase = default!;
    [Dependency] private DoAfterSystem _doAfter = default!;
    [Dependency] private EmpSystem _emp = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultRuneApocalypseComponent, TryInvokeCultRuneEvent>(OnApocalypseRuneInvoked);
        SubscribeLocalEvent<CultRuneApocalypseComponent, ApocalypseRuneDoAfter>(OnDoAfter);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CultRuneApocalypseComponent, CultRuneBaseComponent>();
        while (query.MoveNext(out var uid, out var apocalypse, out var rune))
        {
            if (!apocalypse.Invoking || apocalypse.ChantPhrases.Count == 0)
                continue;

            if (_timing.CurTime < apocalypse.NextChantTime)
                continue;

            apocalypse.NextChantTime = _timing.CurTime + apocalypse.ChantInterval;
            var phrase = apocalypse.ChantPhrases[apocalypse.ChantIndex % apocalypse.ChantPhrases.Count];
            apocalypse.ChantIndex++;

            // Only the original participants who remain around the rune continue the chant.
            var nearbyCultists = _runeBase.GatherCultists(uid, rune.RuneActivationRange);
            foreach (var chanter in apocalypse.Chanters)
            {
                if (!nearbyCultists.Contains(chanter) || TerminatingOrDeleted(chanter))
                    continue;

                _chat.TrySendInGameICMessage(
                    chanter,
                    phrase,
                    InGameICChatType.Speak,
                    false,
                    checkRadioPrefix: false);
            }
        }
    }

    private void OnApocalypseRuneInvoked(Entity<CultRuneApocalypseComponent> ent, ref TryInvokeCultRuneEvent args)
    {
        if (ent.Comp.Used || ent.Comp.Invoking)
        {
            args.Cancel();
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.InvokeTime, new ApocalypseRuneDoAfter(), ent)
        {
            BreakOnMove = true,
            BlockDuplicate = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            args.Cancel();
            return;
        }

        ent.Comp.Invoking = true;
        ent.Comp.NextChantTime = _timing.CurTime + ent.Comp.ChantInterval;
        ent.Comp.ChantIndex = 0;
        ent.Comp.Chanters.Clear();
        ent.Comp.Chanters.UnionWith(args.Invokers);
    }

    private void OnDoAfter(Entity<CultRuneApocalypseComponent> ent, ref ApocalypseRuneDoAfter args)
    {
        ent.Comp.Invoking = false;
        ent.Comp.Chanters.Clear();

        if (args.Cancelled || EntityQuery<BloodCultRuleComponent>().FirstOrDefault() is not { } cultRule)
            return;

        ent.Comp.Used = true;
        _appearance.SetData(ent, ApocalypseRuneVisuals.Used, true);

        _emp.EmpPulse(
            _transform.GetMapCoordinates(ent),
            ent.Comp.EmpRange,
            ent.Comp.EmpEnergyConsumption,
            ent.Comp.EmpDuration); // Dumont

        foreach (var guaranteedEvent in ent.Comp.GuaranteedEvents)
            _gameTicker.StartGameRule(guaranteedEvent);

        // Dumont
        var requiredCultistsThreshold = MathF.Floor(_cultRule.GetProgressionCrewCount() * ent.Comp.CultistsThreshold);
        var totalCultists = cultRule.Cultists.Count + cultRule.Constructs.Count;
        if (totalCultists >= requiredCultistsThreshold)
            return;

        var (randomEvent, repeatTimes) = _random.Pick(ent.Comp.PossibleEvents);
        for (var i = 0; i < repeatTimes; i++)
            _gameTicker.StartGameRule(randomEvent);
    }
}
