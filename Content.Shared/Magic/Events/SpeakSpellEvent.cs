// SPDX-License-Identifier: AGPL-3.0-or-later
// Blood Cult: ported from WWhiteDreamProject/wwdpublic. See Content.Shared/WhiteDream/BloodCult/ATTRIBUTION.md

using Content.Shared.Chat;

namespace Content.Shared.Magic.Events;

[ByRefEvent]
public readonly struct SpeakSpellEvent(EntityUid performer, string speech, InGameICChatType chatType)
{
    public readonly EntityUid Performer = performer;
    public readonly string Speech = speech;
    public readonly InGameICChatType ChatType = chatType;
}
