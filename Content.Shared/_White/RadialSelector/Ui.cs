using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._White.RadialSelector;

[NetSerializable, Serializable]
public enum RadialSelectorUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RadialSelectorState(List<RadialSelectorEntry> entries, bool openCentered = false)
    : BoundUserInterfaceState
{
    public List<RadialSelectorEntry> Entries = entries;

    public bool OpenCentered { get; } = openCentered;
}

[Serializable, NetSerializable]
public sealed class TrackedRadialSelectorState(List<RadialSelectorEntry> entries, NetEntity? trackedEntity = null)
    : BoundUserInterfaceState
{
    public List<RadialSelectorEntry> Entries = entries;

    public NetEntity? TrackedEntity { get; } = trackedEntity;
}

[Serializable, NetSerializable]
public sealed class RadialSelectorSelectedMessage(string selectedItem) : BoundUserInterfaceMessage
{
    public string SelectedItem { get; private set; } = selectedItem;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class RadialSelectorEntry
{
    [DataField]
    public string? Prototype { get; set; }

    // <WhiteDream> - Blood Cult
    [DataField]
    public string? Name { get; set; }

    /// <summary>
    ///     How many copies the server should produce when this entry is selected.
    /// </summary>
    [DataField]
    public int Amount { get; set; } = 1;
    // </WhiteDream>

    [DataField]
    public SpriteSpecifier? Icon { get; set; }

    [DataField]
    public RadialSelectorCategory? Category { get; set; }

    [DataField]
    public bool CloseUiOnSelect = true;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class RadialSelectorCategory
{
    public string Name { get; set; } = string.Empty;

    public SpriteSpecifier Icon { get; set; } = default!;

    public List<RadialSelectorEntry> Entries { get; set; } = new();
}
