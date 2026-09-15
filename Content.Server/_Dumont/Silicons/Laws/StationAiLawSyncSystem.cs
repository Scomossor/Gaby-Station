// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Radio.EntitySystems;
using Content.Server.Silicons.Laws;
using Content.Server.Station.Systems;
using Content.Server._DV.Silicons.Laws;
using Content.Shared._DV.Silicons.Laws;
using Content.Shared.GameTicking;
using Content.Shared.Radio;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Dumont.Silicons.Laws;

/// <summary>
/// espelha as leis da IA nos borgs escravizados a ela
/// o upstream já faz os borgs da NT obedecerem a IA mas só por uma lei estática. o conjunto
/// de leis real nunca chegava neles, então uma IA com ion storm mandava em borg rodando
/// Crewsimov..
/// de propósito não engancha na mudança de lei: compara por tempo, o que dá o atraso que o
/// balanço precisa. IA subvertida não vira o departamento inteiro no mesmo fôlego, e a
/// estação recebe aviso no rádio enquanto acontece
/// </summary>
public sealed class StationAiLawSyncSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly SiliconLawSystem _laws = default!;
    [Dependency] private readonly SlavedBorgSystem _slaved = default!;
    [Dependency] private readonly StationSystem _station = default!;

    /// <summary>
    /// quanto tempo as leis da IA precisam ficar paradas antes de chegar nos borgs. é a
    /// janela que a tripulação tem pra perceber e cortar o vínculo
    /// </summary>
    private static readonly TimeSpan SyncDelay = TimeSpan.FromSeconds(45);

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(5);

    private const string AnnouncementChannel = "Science";

    private TimeSpan _nextCheck;

    /// <summary>
    /// última assinatura de leis conhecida por IA e quando mudou
    /// </summary>
    private readonly Dictionary<EntityUid, (string Signature, TimeSpan ChangedAt, bool Propagated)> _seen = new();

    /// <summary>
    /// o último lawset que cada IA entregou. é contra ele que os borgs se alinham, então
    /// mudança pendente nos 45s não vaza pros borgs antes da hora
    /// </summary>
    private readonly Dictionary<EntityUid, SiliconLawset> _delivered = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        _seen.Clear();
        _delivered.Clear();
        _nextCheck = TimeSpan.Zero;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        if (now < _nextCheck)
            return;

        _nextCheck = now + CheckInterval;

        var query = EntityQueryEnumerator<StationAiHeldComponent, SiliconLawProviderComponent>();
        while (query.MoveNext(out var ai, out _, out _))
        {
            Track(ai, now);

            // realinha em silêncio qualquer borg que destoe do último lawset entregue. isso
            // cobre borg que nasceu depois, troca de chassi resetando as leis, e o que mais
            // inventarem de reset.. um mecanismo só, sempre conferindo
            if (_delivered.TryGetValue(ai, out var delivered))
                Propagate(ai, delivered, announce: false);
        }
    }

    private void Track(EntityUid ai, TimeSpan now)
    {
        var laws = _laws.GetLaws(ai);
        var signature = Signature(laws);

        if (!_seen.TryGetValue(ai, out var previous))
        {
            // primeira vista: alinha os borgs já, senão a IA nasce com o lawset dela e o borg
            // fica no dele até alguém mudar uma lei. silencioso porque isso é o estado natural
            // do coletivo, os avisos da ciência existem pra telegrafar mudança
            _seen[ai] = (signature, now, true);
            Propagate(ai, laws, announce: false);
            return;
        }

        if (previous.Signature != signature)
        {
            _seen[ai] = (signature, now, false);
            Warn(ai, "station-ai-law-sync-detected");
            return;
        }

        if (previous.Propagated || now < previous.ChangedAt + SyncDelay)
            return;

        _seen[ai] = (signature, previous.ChangedAt, true);
        Propagate(ai, laws);
    }

    /// <summary>
    /// empurra as leis da IA pra todo borg escravizado na mesma estação
    /// aqui é fail-closed de propósito, IA sem estação não manda lei pra ninguém. o filtro
    /// de alarme é fail-open porque ouvir um alarme a mais não custa nada, lei custa
    /// borg subvertido (emag) fica de fora, senão a sincronia sobrescreveria o lawset do
    /// traitor 45s depois e o emag deixaria de ser o jeito de cortar o vínculo
    /// </summary>
    private void Propagate(EntityUid ai, SiliconLawset laws, bool announce = true)
    {
        var station = _station.GetOwningStation(ai);
        if (station == null)
            return;

        var count = 0;

        var query = EntityQueryEnumerator<SlavedBorgComponent, SiliconLawProviderComponent>();
        while (query.MoveNext(out var borg, out var slaved, out var provider))
        {
            if (provider.Subverted)
                continue;

            if (_station.GetOwningStation(borg) != station)
                continue;

            if (!_proto.TryIndex(slaved.Law, out var slaveLawProto))
                continue;

            var copy = laws.Laws.Select(law => law.ShallowClone()).ToList();
            copy.RemoveAll(law => law.LawString == slaveLawProto.LawString);

            var lawset = new SiliconLawset { Laws = copy };
            _slaved.AddLaw(lawset, slaved.Law);

            // só escreve quando destoa, senão o borg levaria aviso de lei nova a cada 5s
            if (provider.Lawset != null && Signature(provider.Lawset) == Signature(lawset))
                continue;

            _laws.SetLaws(lawset.Laws, borg);
            count++;
        }

        _delivered[ai] = new SiliconLawset { Laws = laws.Laws.Select(law => law.ShallowClone()).ToList() };

        if (count > 0 && announce)
            Warn(ai, "station-ai-law-sync-applied");
    }

    /// <summary>
    /// mudança de lei de silicon tem que ser perceptível, anunciar no canal da ciência é o
    /// contrajogo.. a robótica consegue tirar um borg do vínculo antes ou depois de cair
    /// </summary>
    private void Warn(EntityUid ai, LocId message)
    {
        _radio.SendRadioMessage(ai, Loc.GetString(message), AnnouncementChannel, ai);
    }

    private static string Signature(SiliconLawset laws)
    {
        return string.Join("", laws.Laws.Select(law => law.LawString));
    }
}
