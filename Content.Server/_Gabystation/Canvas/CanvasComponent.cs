using Content.Server.UserInterface;
using Content.Shared._Gabystation.Canvas;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;

namespace Content.Server._Gabystation.Canvas
{
    [RegisterComponent]
    public sealed partial class CanvasComponent : SharedCanvasComponent
    {
        [DataField("useSound")] public SoundSpecifier? UseSound;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("selectableColor")]
        public bool SelectableColor { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("deleteEmpty")]
        public bool DeleteEmpty = true;
    }
}
