using Content.Client.UserInterface;
using Content.Shared.Ame.Components;
using Content.Shared._Gabystation.Canvas;
using Content.Shared.Medical.CrewMonitoring;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using static Content.Shared._Gabystation.Canvas.SharedCanvasComponent;
using static Robust.Client.UserInterface.Controls.MenuBar;
using System.ComponentModel;
using Color = Robust.Shared.Maths.Color;

namespace Content.Client._Gabystation.Canvas.Ui
{
    public sealed class CanvasBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IPrototypeManager _protoManager = default!;

        [ViewVariables]
        private CanvasWindow? _window;

        public CanvasBoundUserInterface(EntityUid owner, object uiKey) : base(owner, (Enum) uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = this.CreateWindow<CanvasWindow>();
            _window.OnColorSelected += SelectColor;
            _window.OnSelected += Select;
            _window.OnFinalize += Finalize;
            _window.OnClose += Close;
            PopulateCanvas(Owner);
            _window.OpenCentered();
        }

        private void PopulateCanvas(EntityUid uid)
        {
            // Example: A set of predefined colors
            var colors = new List<Color>
            {
                Color.Transparent,
                Color.Red,
                Color.Blue,
                Color.Green,
                Color.Yellow,
                Color.Cyan,
                Color.Magenta,
                new Color(1.0f, 0.65f, 0.0f), // Orange
                new Color(0.75f, 0.0f, 0.75f), // Purple
                new Color(0.33f, 0.55f, 0.2f), // Teal
                new Color(0.6f, 0.3f, 0.1f),   // Brown
                new Color(0.9f, 0.8f, 0.7f),   // Beige
                Color.LightGray,
                Color.DarkGray,
                new Color(0.5f, 0.5f, 1.0f),   // Pastel Blue
                new Color(1.0f, 0.5f, 0.5f),   // Pastel Pink
                new Color(0.0f, 0.5f, 0.5f),   // Dark Cyan
                new Color(0.4f, 0.2f, 0.6f),   // Deep Purple
                Color.Black,
                Color.White // Ensure white is last for consistency
            };


            EntMan.TryGetComponent<CanvasComponent>(Owner, out var canvasComponent);
            if (canvasComponent == null || _window == null)
                return;

            // Set properties from canvasComponent to the window
            _window.SetPaintingCode(canvasComponent?.PaintingCode ?? string.Empty);
            _window.SetHeight(canvasComponent?.Height ?? 16);
            _window.SetWidth(canvasComponent?.Width ?? 16);

            if (!string.IsNullOrEmpty(canvasComponent?.Artist))
            {
                _window.SetArtist(canvasComponent.Artist);
            }
            _window?.PopulateColorSelector(colors);
            _window?.PopulatePaintingGrid();
        }


        public override void OnProtoReload(PrototypesReloadedEventArgs args)
        {
            base.OnProtoReload(args);

            //if (!args.WasModified<>())
            //    return;

            //PopulateCanvas();
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            base.ReceiveMessage(message);

            if (_window is null || message is not CanvasUsedMessage canvasMessage)
                return;

            _window.AdvanceState(canvasMessage.DrawnDecal);
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);


            var castState = (CanvasBoundUserInterfaceState) state;

            _window?.UpdateState(castState);
        }

        public void Select(string state)
        {
            SendMessage(new CanvasSelectMessage(state));
        }

        public void Finalize(string state)
        {
            SendMessage(new CanvasFinalizeMessage(state));
        }

        public void SelectColor(Color color)
        {
            SendMessage(new CanvasColorMessage(color));
        }
    }
}
