using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Heretic;

[RegisterComponent, NetworkedComponent]
public sealed partial class HereticActionComponent : Component
{
    /// <summary>
    ///     Indicates that a user should wear a heretic amulet, a hood or something else.
    /// </summary>
    [DataField] public bool RequireMagicItem = true;
}

#region DoAfters

[Serializable, NetSerializable] public sealed partial class EldritchInfluenceDoAfterEvent : SimpleDoAfterEvent { }
[Serializable, NetSerializable] public sealed partial class DrawRitualRuneDoAfterEvent : SimpleDoAfterEvent
{
    [NonSerialized] public EntityCoordinates Coords;
    [NonSerialized] public EntityUid RitualRune;

    public DrawRitualRuneDoAfterEvent(EntityUid ritualRune, EntityCoordinates coords)
    {
        RitualRune = ritualRune;
        Coords = coords;
    }
}

#endregion

#region Events - Base

public sealed partial class EventHereticOpenStore : InstantActionEvent { }

public sealed partial class EventHereticMansusGrasp : InstantActionEvent { }

#endregion

#region Store - Starting Path

[Serializable, NetSerializable] public sealed partial class KnowledgePathStartAshEvent : EntityEventArgs { }
[Serializable, NetSerializable] public sealed partial class KnowledgePathStartMoonEvent : EntityEventArgs { }
[Serializable, NetSerializable] public sealed partial class KnowledgePathStartLockEvent : EntityEventArgs { }
[Serializable, NetSerializable] public sealed partial class KnowledgePathStartFleshEvent : EntityEventArgs { }
[Serializable, NetSerializable] public sealed partial class KnowledgePathStartVoidEvent : EntityEventArgs { }
[Serializable, NetSerializable] public sealed partial class KnowledgePathStartBladeEvent : EntityEventArgs { }
[Serializable, NetSerializable] public sealed partial class KnowledgePathStartRustEvent : EntityEventArgs { }
[Serializable, NetSerializable] public sealed partial class KnowledgePathStartCosmosEvent : EntityEventArgs { }

#endregion
