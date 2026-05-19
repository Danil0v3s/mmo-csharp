namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Put an inventory item into storage. rAthena
/// <c>clif_parse_MoveToKafra</c> (clif.cpp:13595). Wire:
/// <c>00f3 &lt;index&gt;.W &lt;amount&gt;.L</c> — total 8 bytes.
/// <see cref="ClientIndex"/> is the client-side slot
/// (server_index = client_index − 2).
/// </summary>
public class CZ_MOVE_ITEM_FROM_BODY_TO_STORE : IncomingPacket
{
    private const int SIZE = 8;

    public ushort ClientIndex { get; private set; }
    public int Amount { get; private set; }

    public CZ_MOVE_ITEM_FROM_BODY_TO_STORE()
        : base(PacketHeader.CZ_MOVE_ITEM_FROM_BODY_TO_STORE, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        ClientIndex = reader.ReadUInt16();
        Amount = reader.ReadInt32();
    }

    public static CZ_MOVE_ITEM_FROM_BODY_TO_STORE Create(BinaryReader reader)
    {
        var packet = new CZ_MOVE_ITEM_FROM_BODY_TO_STORE();
        packet.Read(reader);
        return packet;
    }
}
