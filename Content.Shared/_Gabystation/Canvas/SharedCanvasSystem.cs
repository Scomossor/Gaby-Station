using Content.Shared.Interaction.Events;
using Content.Shared.Interaction;
using Robust.Shared.Prototypes;
using Content.Shared.Popups;
using Robust.Shared.GameObjects;
using static Content.Shared._Gabystation.Canvas.SharedCanvasComponent;
using Robust.Shared.GameStates;

namespace Content.Shared._Gabystation.Canvas
{
    [Virtual]
    public abstract partial class SharedCanvasSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();
        }
    }
}
