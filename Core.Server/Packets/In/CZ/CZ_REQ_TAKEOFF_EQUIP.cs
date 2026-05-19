namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Player wants to take off an equipped item. rAthena
/// <c>clif_parse_UnequipItem</c> (clif.cpp:12140) → <c>pc_unequipitem</c>.
/// Wire: <c>00ab &lt;index&gt;.W</c> — total 4 bytes.
/// </summary>
public class CZ_REQ_TAKEOFF_EQUIP : IncomingPacket
{
    private const int SIZE = 4;

    public ushort ClientIndex { get; private set; }

    public CZ_REQ_TAKEOFF_EQUIP() : base(PacketHeader.CZ_REQ_TAKEOFF_EQUIP, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        ClientIndex = reader.ReadUInt16();
    }

    public static CZ_REQ_TAKEOFF_EQUIP Create(BinaryReader reader)
    {
        var packet = new CZ_REQ_TAKEOFF_EQUIP();
        packet.Read(reader);
        return packet;
    }
}
