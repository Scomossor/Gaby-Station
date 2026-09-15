// SPDX-License-Identifier: AGPL-3.0-or-later
// Blood Cult: ported from WWhiteDreamProject/wwdpublic. See Content.Shared/WhiteDream/BloodCult/ATTRIBUTION.md

using Content.Shared.Chat;

namespace Content.Shared.Magic;

public interface ISpeakSpell // The speak n spell interface
{
    /// <summary>
    /// Localized string spoken by the caster when casting this spell.
    /// </summary>
    public string? Speech { get; }

    public InGameICChatType ChatType { get; }
}
