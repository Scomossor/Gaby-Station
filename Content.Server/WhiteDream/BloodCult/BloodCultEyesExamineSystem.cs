// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.WhiteDream.BloodCult.Gamerule;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;

namespace Content.Server.WhiteDream.BloodCult;

public sealed partial class BloodCultEyesExamineSystem : EntitySystem
{
    private const string EyeSlot = "eyes";

    [Dependency] private BloodCultRuleSystem _cultRule = default!;
    [Dependency] private InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultistComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<BloodCultistComponent> cultist, ref ExaminedEvent args)
    {
        // Only once the eyes have actually been painted. During the warning window there is nothing
        // to see yet.
        if (!_cultRule.TryGetActiveRule(out var rule) || !rule.RedEyesApplied)
            return;

        if (_inventory.TryGetSlotEntity(cultist.Owner, EyeSlot, out _))
            return;

        // Phrased without a name or a pronoun on purpose: it reads the same whoever is being examined.
        args.PushMarkup(Loc.GetString("cult-eyes-examine"));
    }
}
