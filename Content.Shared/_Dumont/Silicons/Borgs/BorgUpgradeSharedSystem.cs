// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Silicons.Borgs.Components;

namespace Content.Shared._Dumont.Silicons.Borgs;

/// <summary>
/// cuida da parte do aprimoramento que mexe em estado restrito do chassi.
/// <see cref="BorgChassisComponent.MaxModules"/> tem Access limitado e a instalação mora no
/// servidor que o Shared não pode referenciar, então a escrita fica aqui
/// </summary>
public sealed class BorgUpgradeSharedSystem : EntitySystem
{
    /// <summary>
    /// aumenta o chassi em <paramref name="slots"/> baias de módulo de forma permanente
    /// </summary>
    public void GrantModuleSlots(Entity<BorgChassisComponent> ent, int slots)
    {
        if (slots <= 0)
            return;

        ent.Comp.MaxModules += slots;
        Dirty(ent);
    }
}
