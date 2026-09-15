// SPDX-FileCopyrightText: 2026 Space Station 14 Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Trauma.Shared.Physics.ComplexJoint;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;

namespace Content.Trauma.Client.Physics;

public sealed class ContinuousBeamSystem : SharedContinuousBeamSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Timing.IsFirstTimePredicted)
            return;

        if (_player.LocalEntity is not { } player)
            return;

        if (!TryGetGun(player, out var gun))
            return;

        var mousePos = _eye.PixelToMap(_input.MouseScreenPosition);

        if (mousePos.MapId == MapId.Nullspace)
            return;

        var keyFunc = gun.Value.Comp.AltFire ? EngineKeyFunctions.UseSecondary : EngineKeyFunctions.Use;
        var requestFire = CanFire(player, gun.Value) && _inputSystem.CmdStates.GetState(keyFunc) == BoundKeyState.Down;

        var coordinates = Xform.ToCoordinates(gun.Value.Owner, mousePos);

        RaisePredictiveEvent(new ContinuousBeamPositionEvent(GetNetCoordinates(coordinates), requestFire));
    }
}
