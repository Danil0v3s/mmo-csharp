namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Player wants to equip an inventory item. rAthena
/// <c>clif_parse_EquipItem</c> (clif.cpp:12083) → <c>pc_equipitem</c>.
/// Wire (PACKETVER ≥ 20120925 — CZ_REQ_WEAR_EQUIP_V5):
/// <c>0998 &lt;index&gt;.W &lt;position&gt;.L</c> — total 8 bytes.
/// <see cref="ClientIndex"/> is the client-side slot (server_index = client_index − 2);
/// <see cref="Position"/> is the EQP_* bitmask of the slots the player
/// is requesting to wear into.
/// </summary>
public class CZ_REQ_WEAR_EQUIP : IncomingPacket
{
    private const int SIZE = 8;

    public ushort ClientIndex { get; private set; }
    public uint Position { get; private set; }

    public CZ_REQ_WEAR_EQUIP() : base(PacketHeader.CZ_REQ_WEAR_EQUIP, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        ClientIndex = reader.ReadUInt16();
        Position = reader.ReadUInt32();
    }

    public static CZ_REQ_WEAR_EQUIP Create(BinaryReader reader)
    {
        var packet = new CZ_REQ_WEAR_EQUIP();
        packet.Read(reader);
        return packet;
    }
}
