// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Trauma.Server.Speech;

/// <summary>
/// Replaces individual characters with random strings, ignoring case etc.
/// </summary>
/// <remarks>
/// </remarks>
[RegisterComponent, Access(typeof(CharactersAccentSystem))]
public sealed partial class CharactersAccentComponent : Component
{
    [DataField(required: true)]
    public Dictionary<char, List<string>> Chars = new();
}
