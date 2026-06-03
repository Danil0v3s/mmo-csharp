namespace Core.Server.Packets.In.CZ;

/// <summary>
/// RODEX: stage an inventory item onto the mail draft. rAthena <c>clif_parse_Mail_setattach</c>
/// (clif.cpp:16702) + <c>PACKET_CZ_ADD_ITEM_TO_MAIL</c>. Wire: <c>0a04 &lt;index&gt;.W &lt;count&gt;.W</c> — 6
/// bytes. <see cref="ClientIndex"/> is the client slot (server_index = client_index − 2).
/// </summary>
public class CZ_REQ_ADD_ITEM_TO_MAIL : IncomingPacket
{
    private const int SIZE = 6;

    public ushort ClientIndex { get; private set; }
    public short Count { get; private set; }

    public CZ_REQ_ADD_ITEM_TO_MAIL() : base(PacketHeader.CZ_REQ_ADD_ITEM_TO_MAIL, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        ClientIndex = reader.ReadUInt16();
        Count = reader.ReadInt16();
    }

    public static CZ_REQ_ADD_ITEM_TO_MAIL Create(BinaryReader reader)
    {
        var packet = new CZ_REQ_ADD_ITEM_TO_MAIL();
        packet.Read(reader);
        return packet;
    }
}
