using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using System.IO;

namespace Content.Shared.MOTD;

public sealed class MsgMOTD : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.String;

    public string MOTD = string.Empty;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        MOTD = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(MOTD);
    }

}

public sealed class MsgMOTDRequest : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {

    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {

    }

}
