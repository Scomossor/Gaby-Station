using Content.Shared.Storage;
using Robust.Shared.Network;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Gabystation.Xmas;

/// <summary>
/// This is used as a marker component, allows them to see gift contents.
/// </summary>
[RegisterComponent, Access(typeof(SantaBagSystem))]
public sealed partial class SantaBagComponent : Component
{
    /// <summary>
    /// Selects what entities can be given out by the giver.
    /// </summary>
    [DataField("spawnEntries", required: true)]
    public List<EntitySpawnEntry> SpawnEntries = default!;

    /// <summary>
    /// The currently selected entity to give out. Used so contents viewers can see inside.
    /// </summary>
    [DataField("uses"), ViewVariables(VVAccess.ReadWrite)]
    public int? Uses = default!;
}
