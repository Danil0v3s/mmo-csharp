namespace Core.Server.Packets.In.CZ;

/// <summary>
/// "Use item" — rAthena <c>clif_parse_UseItem</c> (clif.cpp:12051).
/// Wire format: <c>00a7 &lt;index&gt;.W &lt;account id&gt;.L</c>.
/// Total: 2 (header) + 2 (slot index) + 4 (account id) = 8 bytes.
/// </summary>
public class CZ_USE_ITEM : IncomingPacket
{
    private const int SIZE = 8;

    /// <summary>Server-side inventory slot index = client_index − 2 (rAthena <c>server_index</c>).</summary>
    public short Index { get; private set; }
    public int AccountId { get; private set; }

    public CZ_USE_ITEM() : base(PacketHeader.CZ_USE_ITEM, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        Index = reader.ReadInt16();
        AccountId = reader.ReadInt32();
    }

    public static CZ_USE_ITEM Create(BinaryReader reader)
    {
        var packet = new CZ_USE_ITEM();
        packet.Read(reader);
        return packet;
    }
}
