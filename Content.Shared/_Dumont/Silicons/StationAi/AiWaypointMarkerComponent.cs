// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Silicons.StationAi;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Dumont.Silicons.StationAi;

/// <summary>
/// marca o holograma de ponto e lembra de qual IA ele é
/// precisa ser shared porque o menu radial da IA é montado no cliente
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AiWaypointMarkerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Ai;
}

/// <summary>
/// escolhida no radial do holograma, remove o ponto
/// </summary>
[Serializable, NetSerializable]
public sealed class AiWaypointRemoveEvent : BaseStationAiAction;

/// <summary>
/// põe a opção de remover no radial que abre quando a IA clica no holograma
/// o evento de coleta dispara no cliente, por isso mora no shared
/// </summary>
public sealed class AiWaypointMarkerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AiWaypointMarkerComponent, GetStationAiRadialEvent>(OnGetRadial);
    }

    private void OnGetRadial(Entity<AiWaypointMarkerComponent> ent, ref GetStationAiRadialEvent args)
    {
        args.Actions.Add(new StationAiRadial
        {
            Sprite = new SpriteSpecifier.Rsi(new ResPath("_Dumont/Markers/waypoint.rsi"), "pin"),
            Tooltip = Loc.GetString("station-ai-waypoint-remove-verb"),
            Event = new AiWaypointRemoveEvent(),
        });
    }
}
