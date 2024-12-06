using Content.Server.Chat.Systems;
using Content.Server.Station.Components;
using Robust.Shared.Audio;
using Robust.Shared.Map.Components;
using Content.Server.Light.Components;
using Content.Shared.Light.Components;
using Robust.Server.GameObjects;
using System.Text.RegularExpressions;
using System.Globalization;
using Content.Shared.Coordinates;
using Content.Server.Light.EntitySystems;
using Content.Server.Configurable;
using Robust.Shared.Configuration;
using Content.Shared.CCVar;
using FastAccessors;
using Content.Server.GameTicking;
using System.Diagnostics;
using System.Linq;

namespace Content.Server.Time
{
    public sealed partial class LightCycleSystem : EntitySystem
    {
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly IConfigurationManager _configuration = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly PointLightSystem _pointLight = default!;
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
        [Dependency] private readonly GameTicker _gameTicker = default!;
        [Dependency] private readonly PoweredLightSystem? _lightSystem = default!;
        private int _currentHour;
        private double _tickCount = 0;
        private string? _hexColor = "#FFFFFF";
        private bool _isNight = false;
        private Dictionary<int, Color>? _mapColor = new Dictionary<int, Color>();
        private static readonly Regex? HexPattern = new Regex(@"^#([A-Fa-f0-9]){6}$");
        private static readonly SoundSpecifier? NightAlert = new SoundPathSpecifier("/Audio/_Gabystation/Announcements/nightshift.ogg");
        private static readonly SoundSpecifier? DayAlert = new SoundPathSpecifier("/Audio/_Gabystation/Announcements/dayshift.ogg");

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BecomesStationComponent, ComponentInit>(OnStationInit);
            SubscribeLocalEvent<PoweredLightComponent, ComponentStartup>(OnLightBulbStartup);
            SubscribeLocalEvent<PoweredLightComponent, ComponentRemove>(OnLightBulbShutdown);
        }

        private void OnStationInit(Entity<BecomesStationComponent> ent, ref ComponentInit args)
        {
            EnsureComp<LightCycleComponent>(ent);
            _entityManager.GetComponent<LightCycleComponent>(ent).IsEnabled = _configuration.GetCVar(CCVars.CycleEnabled);
            _entityManager.GetComponent<LightCycleComponent>(ent).IsColorShiftEnabled = _configuration.GetCVar(CCVars.ColorEnabled);
            _entityManager.GetComponent<LightCycleComponent>(ent).IsAnnouncementEnabled = _configuration.GetCVar(CCVars.AnnouncementEnabled);
        }

        private void OnLightBulbStartup(Entity<PoweredLightComponent> ent, ref ComponentStartup args)
        {
            var station = _transformSystem.GetGrid(ent.Owner);
            if (EntityManager.TryGetComponent<LightCycleComponent>(station, out var cycle) && cycle.IsEnabled)
            {
                if (!cycle.BulbList.Contains(ent))
                    cycle.BulbList.Add(ent);
            }
        }

        private void OnLightBulbShutdown(Entity<PoweredLightComponent> ent, ref ComponentRemove args)
        {
            var station = _transformSystem.GetGrid(ent.Owner);
            if (EntityManager.TryGetComponent<LightCycleComponent>(station, out var cycle) && cycle.IsEnabled)
            {
                if (cycle.BulbList.Contains(ent))
                    cycle.BulbList.Remove(ent);
            }
        }

        public double GetStationTime()
        {
            var roundDuration = _gameTicker.RoundDuration().TotalSeconds;
            return roundDuration * _configuration.GetCVar(CCVars.TimeScale) + _configuration.GetCVar(CCVars.InitialTime) * _configuration.GetCVar(CCVars.TimeScale);
        }
        public override void Update(float frameTime)
        {
            _tickCount++;
            var updatePerTick = _configuration.GetCVar(CCVars.UpdatePerTick);

            if (updatePerTick == 0 || _tickCount % updatePerTick != 0)
                return;

            _tickCount = 0;
            _currentHour = TimeSpan.FromSeconds(GetStationTime()).Hours;
            foreach (var comp in EntityQuery<LightCycleComponent>())
            {
                if (comp.IsEnabled && EntityManager.TryGetComponent<BecomesStationComponent>(comp.Owner, out var station))
                {
                    if ((_currentHour >= comp.NightShiftStart || _currentHour < TimeSpan.FromHours(comp.NightShiftStart + comp.NightShiftDuration).Hours) && !_isNight)
                    {
                        if (comp.IsAnnouncementEnabled)
                            _chatSystem.DispatchStationAnnouncement(station.Owner,
                            Loc.GetString("light-cycle-night"),
                            Loc.GetString("comms-console-announcement-title-centcom"),
                            true, NightAlert, colorOverride: Color.SkyBlue);

                        _isNight = true;
                    }
                    else if (_currentHour >= TimeSpan.FromHours(comp.NightShiftStart + comp.NightShiftDuration).Hours && _currentHour < comp.NightShiftStart && _isNight)
                    {
                        if (comp.IsAnnouncementEnabled)
                            _chatSystem.DispatchStationAnnouncement(station.Owner,
                            Loc.GetString("light-cycle-day"),
                            Loc.GetString("comms-console-announcement-title-centcom"),
                            true, DayAlert, colorOverride: Color.OrangeRed);

                        _isNight = false;
                    }

                    foreach (var light in comp.BulbList.ToList())
                    {
                        var bulbUid = _lightSystem!.GetBulb(light, light.Comp);

                        if (bulbUid is null)
                        {
                            comp.BulbList.Remove(light);
                            continue;
                        }

                        if (!EntityManager.TryGetComponent<LightBulbComponent>(bulbUid, out var bulb)
                        || HasComp<RgbLightControllerComponent>(light))
                            continue;

                        var color = bulb.Color;

                        if (comp.IsOverrideEnabled)
                        {
                            if (_hexColor != comp.OverrideColor)
                            {
                                var match = HexPattern!.Match(comp.OverrideColor);
                                if (match.Success)
                                    _hexColor = comp.OverrideColor;
                            }
                            color = System.Drawing.Color.FromArgb(int.Parse(_hexColor!.Replace("#", ""), NumberStyles.HexNumber));
                        }

                        if (EntityManager.TryGetComponent(light, out PointLightComponent? pointLight))
                        {
                            _pointLight.SetColor(light, GetCycleColor(comp, color), pointLight);
                            _pointLight.SetEnergy(light, (float) CalculateLightLevel(comp), pointLight);
                        }
                    }
                }

                if (EntityManager.TryGetComponent<MapLightComponent>(comp.Owner, out var map))
                {
                    if (!_mapColor!.ContainsKey(map.Owner.Id))
                        _mapColor.Add(map.Owner.Id, map.AmbientLightColor);
                    else
                    {
                        var lightLevel = CalculateLightLevel(comp);
                        var color = GetCycleColor(comp, _mapColor[map.Owner.Id]);
                        var red = (int) Math.Min(255, color.RByte * lightLevel);
                        var green = (int) Math.Min(255, color.GByte * lightLevel);
                        var blue = (int) Math.Min(255, color.BByte * lightLevel);
                        map.AmbientLightColor = System.Drawing.Color.FromArgb(red, green, blue);
                        Dirty(map.Owner, map);
                    }
                }
            }
        }

        public Color GetCycleColor(LightCycleComponent comp, Color color)
        {
            if (comp.IsEnabled && comp.IsColorShiftEnabled)
            {
                var colorLevel = CalculateColorLevel(comp);
                var red = Math.Min(255, color.RByte * colorLevel[0]);
                var green = Math.Min(255, color.GByte * colorLevel[1]);
                var blue = Math.Min(255, color.BByte * colorLevel[2]);
                return System.Drawing.Color.FromArgb((int) red, (int) green, (int) blue);
            }
            else
                return color;

        }

        public double CalculateLightLevel(LightCycleComponent comp)
        {
            var time = GetStationTime();
            var wave_lenght = Math.Max(0, comp.CycleDuration) * 24;
            var crest = Math.Max(1, comp.MaxLightLevel);
            var shift = Math.Max(0, comp.MinLightLevel);
            return Math.Min(comp.ClipLight, CalculateCurve(time, wave_lenght, crest, shift, 6));
        }

        public double[] CalculateColorLevel(LightCycleComponent comp)
        {
            var wave_lenght = Math.Max(0, comp.CycleDuration) * 24;
            var time = GetStationTime();
            var color_level = new double[3];
            for (var i = 0; i < 3; i++)
            {
                switch (i)
                {
                    case 0:
                        color_level[i] = Math.Min(comp.ClipRed, CalculateCurve(time, wave_lenght,
                        Math.Max(0, comp.MaxRedLevel), Math.Max(0, comp.MinRedLevel), 4));
                        break;
                    case 1:
                        color_level[i] = Math.Min(comp.ClipGreen, CalculateCurve(time, wave_lenght,
                        Math.Max(0, comp.MaxGreenLevel), Math.Max(0, comp.MinGreenLevel), 10));
                        break;
                    case 2:
                        color_level[i] = Math.Min(comp.ClipBlue, CalculateCurve(time, wave_lenght / 2,
                        Math.Max(0, comp.MaxBlueLevel), Math.Max(0, comp.MinBlueLevel), 2, wave_lenght / 4));
                        break;
                }
            }
            return color_level;
        }

        public static double CalculateCurve(double x, double wave_lenght, double crest, double shift, double exponent, double phase = 0)
        {
            var sen = Math.Pow(Math.Sin((Math.PI * (phase + x)) / wave_lenght), exponent);
            return ((crest - shift) * sen) + shift;
        }
    }
}
