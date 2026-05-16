namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Post-char-select TCP connect to map server. Mirrors rAthena
/// <c>clif_parse_WantToConnection</c>. Packet shape for PACKETVER 20211103:
/// 0x0436 packet_id (2) + account_id (4) + char_id (4) + login_id1 (4) + client_tick (4) + sex (1) = 19 bytes.
/// </summary>
public class CZ_WANT_TO_CONNECTION : IncomingPacket
{
    private const int SIZE = 19;

    public int AccountId { get; private set; }
    public int CharacterId { get; private set; }
    public int LoginId1 { get; private set; }
    public uint ClientTick { get; private set; }
    public byte Sex { get; private set; }

    public CZ_WANT_TO_CONNECTION() : base(PacketHeader.CZ_WANT_TO_CONNECTION, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        AccountId = reader.ReadInt32();
        CharacterId = reader.ReadInt32();
        LoginId1 = reader.ReadInt32();
        ClientTick = reader.ReadUInt32();
        Sex = reader.ReadByte();
    }

    public static CZ_WANT_TO_CONNECTION Create(BinaryReader reader)
    {
        var packet = new CZ_WANT_TO_CONNECTION();
        packet.Read(reader);
        return packet;
    }
}
