// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.UserInterface;
using Content.Shared.WhiteDream.BloodCult;
using Content.Shared._White.RadialSelector;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;

namespace Content.Server.WhiteDream.BloodCult.TimedFactory;

public sealed partial class TimedFactorySystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TimedFactoryComponent, ActivatableUIOpenAttemptEvent>(OnTryOpenMenu);
        SubscribeLocalEvent<TimedFactoryComponent, RadialSelectorSelectedMessage>(OnPrototypeSelected);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var factoryQuery = EntityQueryEnumerator<TimedFactoryComponent>();
        while (factoryQuery.MoveNext(out var uid, out var factory))
            if (factory.CooldownRemaining > 0)
                factory.CooldownRemaining -= frameTime;
            else
                _appearance.SetData(uid, GenericCultVisuals.State, true);
    }

    private void OnTryOpenMenu(Entity<TimedFactoryComponent> factory, ref ActivatableUIOpenAttemptEvent args)
    {
        var cooldown = MathF.Ceiling(factory.Comp.CooldownRemaining);
        if (cooldown > 0)
        {
            args.Cancel();
            _popup.PopupEntity(Loc.GetString("timed-factory-cooldown", ("cooldown", cooldown)), factory, args.User);
        }

        if (_ui.IsUiOpen(factory.Owner, RadialSelectorUiKey.Key))
            return;

        _ui.SetUiState(factory.Owner, RadialSelectorUiKey.Key, new RadialSelectorState(factory.Comp.Entries));
    }

    private void OnPrototypeSelected(Entity<TimedFactoryComponent> factory, ref RadialSelectorSelectedMessage args)
    {
        if (factory.Comp.CooldownRemaining > 0)
            return;

        // Dumont
        RadialSelectorEntry? entry = null;
        foreach (var candidate in factory.Comp.Entries)
        {
            if (candidate.Prototype != args.SelectedItem)
                continue;

            entry = candidate;
            break;
        }

        if (entry?.Prototype is not { } prototype)
            return;

        var amount = Math.Max(1, entry.Amount);
        for (var i = 0; i < amount; i++)
        {
            var product = Spawn(prototype, Transform(args.Actor).Coordinates);
            if (i == 0)
                _hands.TryPickupAnyHand(args.Actor, product);
        }

        // WhiteDream - each structure has its own voice.
        if (factory.Comp.ProductionSound is { } sound)
            _audio.PlayPvs(sound, factory, AudioParams.Default.WithVolume(-2f));
        factory.Comp.CooldownRemaining = factory.Comp.Cooldown;
        _appearance.SetData(factory, GenericCultVisuals.State, false);
        _ui.CloseUi(args.Actor, RadialSelectorUiKey.Key);
    }
}
