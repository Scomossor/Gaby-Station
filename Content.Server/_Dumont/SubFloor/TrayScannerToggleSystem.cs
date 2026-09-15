// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.SubFloor;
using Content.Shared._Dumont.Overlays;
using Content.Shared._Dumont.SubFloor;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.SubFloor;

namespace Content.Server._Dumont.SubFloor;

/// <summary>
/// liga o scanner de subsolo pelo ItemToggle, que é como roupa vestida é
/// acionada (magboots, capacete de hardsuit). o scanner de mão do upstream só
/// liga por ActivateInWorld, o que exige o item na mão e não serve pra óculos
/// </summary>
public sealed class TrayScannerToggleSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WornTrayScannerComponent, ItemToggledEvent>(OnToggled);
        SubscribeLocalEvent<WornTrayScannerComponent, ActivateInWorldEvent>(OnActivate,
            before: [typeof(TrayScannerSystem)]);
        SubscribeLocalEvent<WornTrayScannerComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
        SubscribeLocalEvent<MesonVisionComponent, ItemToggleActivateAttemptEvent>(OnMesonActivateAttempt);
        SubscribeLocalEvent<ScannerVisionComponent, ItemToggledEvent>(OnVisionToggled);
        SubscribeLocalEvent<MesonVisionComponent, ItemToggledEvent>(OnMesonToggled);
    }

    private void OnMesonToggled(Entity<MesonVisionComponent> ent, ref ItemToggledEvent args)
    {
        if (ent.Comp.Enabled == args.Activated)
            return;

        ent.Comp.Enabled = args.Activated;
        Dirty(ent);
    }

    /// <summary>
    /// o toggle liga só o que o óculos declarou saber fazer no prototype
    /// </summary>
    private void OnVisionToggled(Entity<ScannerVisionComponent> ent, ref ItemToggledEvent args)
    {
        var structure = ent.Comp.ShowsStructure && args.Activated;

        if (ent.Comp.Structure == structure)
            return;

        ent.Comp.Structure = structure;
        Dirty(ent);
    }

    private void OnToggled(Entity<WornTrayScannerComponent> ent, ref ItemToggledEvent args)
    {
        if (!TryComp<TrayScannerComponent>(ent, out var scanner) || scanner.Enabled == args.Activated)
            return;

        scanner.Enabled = args.Activated;
        Dirty(ent.Owner, scanner);
    }

    /// <summary>
    /// o clique do upstream ligava o Enabled direto, por fora do ItemToggle.
    /// bloqueado pra sobrar um caminho só, senão os dois brigam pelo estado
    /// </summary>
    private void OnActivate(Entity<WornTrayScannerComponent> ent, ref ActivateInWorldEvent args)
    {
        args.Handled = true;
    }

    /// <summary>
    /// só liga no rosto. o scanner do upstream funciona na mão também, então
    /// sem esse corte o óculos segurado revelaria o subsolo
    /// </summary>
    private void OnActivateAttempt(Entity<WornTrayScannerComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (IsWorn(ent))
            return;

        args.Cancelled = true;
    }

    private void OnMesonActivateAttempt(Entity<MesonVisionComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (IsWorn(ent))
            return;

        args.Cancelled = true;
    }

    private bool IsWorn(EntityUid uid)
    {
        return _inventory.TryGetContainingSlot(uid, out _);
    }
}
