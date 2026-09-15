// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Popups;
using Content.Server.WhiteDream.BloodCult.Empower;

namespace Content.Server.WhiteDream.BloodCult.Runes.Empower;

public sealed partial class CultRuneEmpowerSystem : EntitySystem
{
    [Dependency] private IComponentFactory _factory = default!;

    [Dependency] private PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultRuneEmpowerComponent, TryInvokeCultRuneEvent>(OnStrengthRuneInvoked);
    }

    private void OnStrengthRuneInvoked(Entity<CultRuneEmpowerComponent> ent, ref TryInvokeCultRuneEvent args)
    {
        var registration = _factory.GetRegistration(ent.Comp.ComponentToGive);
        if (HasComp(args.User, registration.Type))
        {
            _popup.PopupEntity(Loc.GetString("cult-buff-already-buffed"), args.User, args.User);
            args.Cancel();
            return;
        }

        var empowered = (BloodCultEmpoweredComponent) _factory.GetComponent(ent.Comp.ComponentToGive);
        empowered.TimeRemaining = empowered.DefaultTime;
        AddComp(args.User, empowered);
    }
}
