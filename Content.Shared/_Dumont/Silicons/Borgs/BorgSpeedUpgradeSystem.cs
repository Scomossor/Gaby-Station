// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Dumont.Silicons.Borgs;

/// <summary>
/// dado pelo aprimoramento VTEC. velocidade é o único efeito que precisa de sistema porque
/// é recalculada por evento em vez de sair direto de um campo
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BorgSpeedUpgradeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float WalkModifier = 1.25f;

    [DataField, AutoNetworkedField]
    public float SprintModifier = 1.25f;
}

public sealed class BorgSpeedUpgradeSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgSpeedUpgradeComponent, RefreshMovementSpeedModifiersEvent>(OnRefresh);
        SubscribeLocalEvent<BorgSpeedUpgradeComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BorgSpeedUpgradeComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<BorgSpeedUpgradeComponent> ent, ref ComponentStartup args)
    {
        _speed.RefreshMovementSpeedModifiers(ent.Owner);
    }

    private void OnShutdown(Entity<BorgSpeedUpgradeComponent> ent, ref ComponentShutdown args)
    {
        _speed.RefreshMovementSpeedModifiers(ent.Owner);
    }

    private void OnRefresh(Entity<BorgSpeedUpgradeComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.WalkModifier, ent.Comp.SprintModifier);
    }
}
