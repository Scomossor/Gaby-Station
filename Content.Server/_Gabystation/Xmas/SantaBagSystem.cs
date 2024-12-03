using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.Storage;
using Robust.Shared.Player;

using Content.Server.Holiday.Christmas;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;

namespace Content.Server._Gabystation.Xmas;

/// <summary>
/// This handles handing out items from the santa bag
/// </summary>
public sealed class SantaBagSystem : EntitySystem
{
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<SantaBagComponent, UseInHandEvent>(OnInteractHand);
    }

    private void OnInteractHand(EntityUid uid, SantaBagComponent component, UseInHandEvent args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        //se tiver o componente de papai noel
        if (!TryComp<SantaComponent>(args.User, out var sActor)) {
            _popup.PopupEntity(Loc.GetString("gs-santabag-noSanta"), uid, args.User);
            return;
        }

        if(component.Uses <= 0) {
            _popup.PopupEntity(Loc.GetString("gs-santabag-noPresents"), uid, args.User);
            return;
        }

        var toGive = EntitySpawnCollection.GetSpawns(component.SpawnEntries);
        var coords = Transform(args.User).Coordinates;

        foreach (var item in toGive)
        {
            if (item is null)
                continue;

            var spawned = Spawn(item, coords);
            _hands.PickupOrDrop(args.User, spawned);
        }

        component.Uses -= 1;
        _popup.PopupEntity(Loc.GetString("gs-santabag-grabPresent"), uid, args.User);
    }
}
