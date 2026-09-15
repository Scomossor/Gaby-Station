// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.PowerCell;
using Content.Shared._Dumont.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Timing;

namespace Content.Server._Dumont.Silicons.Borgs;

/// <summary>
/// publica a faixa de carga de cada borg pra que os HUDs de diagnóstico consigam mostrar
/// </summary>
public sealed class SiliconChargeStateSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    /// <summary>
    /// carga anda devagar e o HUD é uma olhada, não um instrumento.
    /// </summary>
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(3);

    private TimeSpan _next;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        if (now < _next)
            return;

        _next = now + Interval;

        var query = EntityQueryEnumerator<BorgChassisComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            var level = GetLevel(uid);
            var state = EnsureComp<SiliconChargeStateComponent>(uid);

            if (state.Level == level)
                continue;

            state.Level = level;
            Dirty(uid, state);
        }
    }

    private SiliconChargeLevel GetLevel(EntityUid uid)
    {
        if (!_powerCell.TryGetBatteryFromSlot(uid, out var battery) || battery.MaxCharge <= 0)
            return SiliconChargeLevel.None;

        var percent = battery.CurrentCharge / battery.MaxCharge;

        return percent switch
        {
            <= 0f => SiliconChargeLevel.None,
            < 0.15f => SiliconChargeLevel.Critical,
            < 0.35f => SiliconChargeLevel.Low,
            < 0.60f => SiliconChargeLevel.Half,
            < 0.85f => SiliconChargeLevel.High,
            _ => SiliconChargeLevel.Full,
        };
    }
}
