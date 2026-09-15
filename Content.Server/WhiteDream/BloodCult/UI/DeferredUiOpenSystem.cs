// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.UserInterface;

namespace Content.Server.WhiteDream.BloodCult.UI;

public sealed class DeferredUiOpenSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    private readonly List<(EntityUid Owner, Enum Key, EntityUid Actor)> _pending = new();

    public void OpenNextTick(EntityUid owner, Enum key, EntityUid actor)
    {
        _pending.Add((owner, key, actor));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_pending.Count == 0)
            return;

        foreach (var (owner, key, actor) in _pending)
        {
            if (TerminatingOrDeleted(owner) || TerminatingOrDeleted(actor))
                continue;

            _ui.OpenUi(owner, key, actor);
        }

        _pending.Clear();
    }
}
