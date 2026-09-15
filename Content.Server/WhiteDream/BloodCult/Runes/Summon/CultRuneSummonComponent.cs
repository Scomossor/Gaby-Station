// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Server.WhiteDream.BloodCult.Runes.Summon;

[RegisterComponent]
public sealed partial class CultRuneSummonComponent : Component
{
    [DataField]
    public SoundPathSpecifier TeleportSound = new("/Audio/WhiteDream/BloodCult/veilin.ogg");
}
