// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mind;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Trauma.Shared.EntityConditions;

/// <summary>
/// Checks the target entity against a whitelist or blacklist.
/// </summary>
public sealed partial class WhitelistCondition : TraumaEntityCondition<WhitelistCondition>
{
    /// <summary>
    /// A whitelist the target entity must match if non-null.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// A blacklist the target entity cannot match if non-null.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Guidebook text explaining this whitelist.
    /// </summary>
    [DataField]
    public LocId? GuidebookText;

    /// <summary>
    /// Whether it should also check if mind entity passes for whitelist
    /// </summary>
    [DataField]
    public bool CheckMind;

    public override string GuidebookExplanation(IPrototypeManager prototype)
        => GuidebookText == null ? string.Empty : Loc.GetString(GuidebookText);
}

/// <summary>
/// Handles <see cref="WhitelistCondition"/>.
/// </summary>
public sealed partial class WhitelistConditionSystem : TraumaEntityConditionSystem<MetaDataComponent, WhitelistCondition>
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    protected override bool Condition(Entity<MetaDataComponent> ent, WhitelistCondition condition)
    {
        if (_whitelist.CheckBoth(ent, blacklist: condition.Blacklist, whitelist: condition.Whitelist))
            return true;

        if (!condition.CheckMind)
            return false;

        return _mind.TryGetMind(ent, out var mind, out _) &&
               _whitelist.CheckBoth(mind, blacklist: condition.Blacklist, whitelist: condition.Whitelist);
    }
}
