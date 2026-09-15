// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mobs.Components;

namespace Content.Shared.Mobs.Systems;

public sealed partial class MobThresholdSystem
{
    public void ForceThresholdState(EntityUid target, MobState state, MobThresholdsComponent? thresholds = null, MobStateComponent? mobState = null)
    {
        if (!Resolve(target, ref thresholds, ref mobState, false))
            return;

        thresholds.CurrentThresholdState = state;
        Dirty(target, thresholds);

        _mobStateSystem.ChangeMobState(target, state, mobState);
    }
}
