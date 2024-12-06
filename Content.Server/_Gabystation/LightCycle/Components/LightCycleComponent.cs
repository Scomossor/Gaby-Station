
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
        public int NightShiftStart = 19;
        [ViewVariables(VVAccess.ReadWrite), DataField("nightShiftDuration")]
        public int NightShiftDuration = 9;
        [ViewVariables(VVAccess.ReadWrite), DataField("minLightLevel")]
        public double MinLightLevel = 0.2;
        [ViewVariables(VVAccess.ReadWrite), DataField("maxLightLevel")]
        public double MaxLightLevel = 1.2;
        [ViewVariables(VVAccess.ReadWrite), DataField("clipLight")]
        public double ClipLight = 1;
        [ViewVariables(VVAccess.ReadWrite), DataField("clipRed")]
        public double ClipRed = 1.05;
        [ViewVariables(VVAccess.ReadWrite), DataField("clipGreen")]
        public double ClipGreen = 1;
        [ViewVariables(VVAccess.ReadWrite), DataField("clipBlue")]
        public double ClipBlue = 1.1;
        [ViewVariables(VVAccess.ReadWrite), DataField("minRedLevel")]
        public double MinRedLevel = 0.65;
        [ViewVariables(VVAccess.ReadWrite), DataField("minGreenLevel")]
        public double MinGreenLevel = 0.8;
        [ViewVariables(VVAccess.ReadWrite), DataField("minBlueLevel")]
        public double MinBlueLevel = 0.55;
        [ViewVariables(VVAccess.ReadWrite), DataField("maxRedLevel")]
        public double MaxRedLevel = 2;
        [ViewVariables(VVAccess.ReadWrite), DataField("maxGreenLevel")]
        public double MaxGreenLevel = 2;
        [ViewVariables(VVAccess.ReadWrite), DataField("maxBlueLevel")]
        public double MaxBlueLevel = 4;
        public List<Entity<PoweredLightComponent>> BulbList = new List<Entity<PoweredLightComponent>>();
    }
}
