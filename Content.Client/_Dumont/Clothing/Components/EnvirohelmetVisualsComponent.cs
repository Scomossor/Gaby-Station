using Content.Shared.Hands.Components;

namespace Content.Client.Clothing.Components;

[RegisterComponent]
public sealed partial class EnvirohelmetVisualsComponent : Component
{
    [DataField]
    public string SpriteLayer = "open";

    [DataField]
    public Dictionary<string, List<PrototypeLayerData>> ClothingVisuals = new();

    [DataField]
    public Dictionary<HandLocation, List<PrototypeLayerData>> InhandVisuals = new();
}
