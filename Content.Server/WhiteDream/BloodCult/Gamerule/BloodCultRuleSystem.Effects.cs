// SPDX-License-Identifier: AGPL-3.0-or-later

// WhiteDream - the station itself reacts when the cult moves.
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Light.Components;
using Content.Shared.Parallax;
using Content.Shared.Station.Components;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.WhiteDream.BloodCult.Gamerule;

public sealed partial class BloodCultRuleSystem
{
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!; // Dumont
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private StationSystem _station = default!;

    private const string VeilParallax = "BloodVeilParallax";
    private static readonly Color VeilAmbientLight = Color.FromHex("#2b0808");

    /// <summary>
    ///     Makes every powered light on the station blink for a while.
    /// </summary>
    public void FlickerStationLights(TimeSpan duration)
    {
        // Dumont changes start
        var lights = new List<Entity<PoweredLightComponent>>();
        var query = EntityQueryEnumerator<PoweredLightComponent>();

        while (query.MoveNext(out var uid, out var light))
        {
            lights.Add((uid, light));
            _poweredLight.ToggleBlinkingLight(uid, light, true);
        }

        Timer.Spawn(duration,
            () =>
            {
                foreach (var light in lights)
                {
                    if (TerminatingOrDeleted(light.Owner))
                        continue;

                    _poweredLight.ToggleBlinkingLight(light.Owner, light.Comp, false);
                }
            });
        // Dumont end
    }

    /// <summary>
    ///     Plays a sound for the whole station.
    /// </summary>
    public void PlayGlobalCultSound(SoundSpecifier sound, float volume = 0f)
    {
        _audio.PlayGlobal(sound, Filter.Broadcast(), true, AudioParams.Default.WithVolume(volume));
    }

    /// <summary>
    ///     Turns the sky over the station red once the veil is torn.
    /// </summary>
    public void StainTheSky(Entity<StationDataComponent> station)
    {
        // The station entity itself lives in nullspace, so take the map from one of its grids.
        foreach (var grid in station.Comp.Grids)
        {
            if (_transform.GetMap(grid) is not { Valid: true } map)
                continue;

            EnsureComp<ParallaxComponent>(map, out var parallax);
            parallax.Parallax = VeilParallax;
            Dirty(map, parallax);

            EnsureComp<MapLightComponent>(map, out var mapLight);
            mapLight.AmbientLightColor = VeilAmbientLight;
            Dirty(map, mapLight);
            return;
        }
    }
}
