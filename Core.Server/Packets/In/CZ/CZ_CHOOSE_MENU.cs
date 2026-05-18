namespace Core.Server.Packets.In.CZ;

/// <summary>
/// "Client picked an option in the open menu." rAthena
/// <c>clif_parse_ChooseMenu</c>. Fixed 7 bytes: 0x00b8 packet_id (2) +
/// npcId (4) + selection (1). Selection is 1-based; 255 means the user
/// closed the menu (Escape).
/// </summary>
public class CZ_CHOOSE_MENU : IncomingPacket
{
    private const int SIZE = 7;

    public uint NpcId { get; private set; }
    public byte Selection { get; private set; }

    public CZ_CHOOSE_MENU() : base(PacketHeader.CZ_CHOOSE_MENU, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        NpcId = reader.ReadUInt32();
        Selection = reader.ReadByte();
    }

    public static CZ_CHOOSE_MENU Create(BinaryReader reader)
    {
        var packet = new CZ_CHOOSE_MENU();
        packet.Read(reader);
        return packet;
    }
}
