namespace Core.Server.Packets.In.CZ;

/// <summary>
/// "I clicked an NPC." rAthena <c>clif_parse_NpcClicked</c>
/// (<c>struct PACKET_CZ_CONTACTNPC</c>). Fixed 7 bytes:
/// packetType(2) + npcId(4) + type(1).
///
/// The <c>type</c> byte is 0 from official clients (legacy field — used by
/// the long-defunct "right-click" disambiguation). The NPC id is the
/// entity id the client received in a prior <see cref="Out.ZC.ZC_NOTIFY_STANDENTRY"/>
/// AccountId / GID field.
/// </summary>
public class CZ_CONTACTNPC : IncomingPacket
{
    private const int SIZE = 7;

    public uint NpcId { get; private set; }
    public byte ClickType { get; private set; }

    public CZ_CONTACTNPC() : base(PacketHeader.CZ_CONTACTNPC, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        NpcId = reader.ReadUInt32();
        ClickType = reader.ReadByte();
    }

    public static CZ_CONTACTNPC Create(BinaryReader reader)
    {
        var packet = new CZ_CONTACTNPC();
        packet.Read(reader);
        return packet;
    }
}
