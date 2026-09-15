// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.WhiteDream.BloodCult.Gamerule;
using Content.Shared.UserInterface;
using Content.Shared.WhiteDream.BloodCult.Runes;
using Robust.Shared.Prototypes;

namespace Content.Server.WhiteDream.BloodCult.Runes;

/// <summary>
///     Builds the rune menu on the server, using the gamerule's authoritative cultist count.
/// </summary>
public sealed partial class RuneDrawerSystem : EntitySystem
{
    [Dependency] private BloodCultRuleSystem _cultRule = default!;
    [Dependency] private IPrototypeManager _protoManager = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RuneDrawerComponent, BeforeActivatableUIOpenEvent>(OnBeforeUiOpen);
    }

    private void OnBeforeUiOpen(Entity<RuneDrawerComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        var availableRunes = new List<ProtoId<RuneSelectorPrototype>>();
        var totalCultists = _cultRule.GetTotalCultists();

        foreach (var runeSelector in _protoManager.EnumeratePrototypes<RuneSelectorPrototype>().OrderBy(r => r.ID))
        {
            if (_cultRule.GetRequiredCultists(runeSelector) > totalCultists)
                continue;

            availableRunes.Add(runeSelector.ID);
        }

        _ui.SetUiState(ent.Owner, RuneDrawerBuiKey.Key, new RuneDrawerMenuState(availableRunes));
    }
}
