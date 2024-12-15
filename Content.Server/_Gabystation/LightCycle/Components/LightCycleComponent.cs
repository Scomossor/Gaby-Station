
using System.Collections;
using System.ComponentModel.DataAnnotations;
using Content.Server.Light.Components;
using Content.Shared.Light.Components;

namespace Content.Server.Time
{
    [RegisterComponent]
    public sealed partial class LightCycleComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite), DataField("isEnabled")]
        public bool IsEnabled = false;
        [ViewVariables(VVAccess.ReadWrite), DataField("isColorEnabled")]
        public bool IsColorShiftEnabled = false;
        [ViewVariables(VVAccess.ReadWrite), DataField("isAnnouncementEnabled")]
        public bool IsAnnouncementEnabled = false;
        [ViewVariables(VVAccess.ReadWrite), DataField("isColorOverrideEnabled")]
        public bool IsOverrideEnabled = false;
        [ViewVariables(VVAccess.ReadWrite), DataField("overrideColor")]
        public string OverrideColor = "#FFFFFF";
        [ViewVariables(VVAccess.ReadWrite), DataField("cycleDuration")]
        public double CycleDuration = 3600;
        [ViewVariables(VVAccess.ReadWrite), DataField("nightShiftStart")]
        public int NightShiftStart = 20;
        [ViewVariables(VVAccess.ReadWrite), DataField("nightShiftDuration")]
        public int NightShiftDuration = 8;
        [ViewVariables(VVAccess.ReadWrite), DataField("minLightLevel")]
        public double MinLightLevel = 0.6;
        [ViewVariables(VVAccess.ReadWrite), DataField("maxLightLevel")]
        public double MaxLightLevel = 1.2;
        [ViewVariables(VVAccess.ReadWrite), DataField("clipLight")]
        public double ClipLight = 0.95;
        [ViewVariables(VVAccess.ReadWrite), DataField("clipRed")]
        public double ClipRed = 1.05;
        [ViewVariables(VVAccess.ReadWrite), DataField("clipGreen")]
        public double ClipGreen = 1;
        [ViewVariables(VVAccess.ReadWrite), DataField("clipBlue")]
        public double ClipBlue = 1.05;
        [ViewVariables(VVAccess.ReadWrite), DataField("minRedLevel")]
        public double MinRedLevel = 0.65;
        [ViewVariables(VVAccess.ReadWrite), DataField("minGreenLevel")]
        public double MinGreenLevel = 0.75;
        [ViewVariables(VVAccess.ReadWrite), DataField("minBlueLevel")]
        public double MinBlueLevel = 0.7;
        [ViewVariables(VVAccess.ReadWrite), DataField("maxRedLevel")]
        public double MaxRedLevel = 1.5;
        [ViewVariables(VVAccess.ReadWrite), DataField("maxGreenLevel")]
        public double MaxGreenLevel = 1.3;
        [ViewVariables(VVAccess.ReadWrite), DataField("maxBlueLevel")]
        public double MaxBlueLevel = 4;
        [ViewVariables(VVAccess.ReadWrite), DataField("exponentRed")]
        public double ExponentRed = 2;
        [ViewVariables(VVAccess.ReadWrite), DataField("exponentGreen")]
        public double ExponentGreen = 4;
        [ViewVariables(VVAccess.ReadWrite), DataField("exponentBlue")]
        public double ExponentBlue = 2;
        public List<Entity<PoweredLightComponent>> BulbList = new List<Entity<PoweredLightComponent>>();
    }
}
