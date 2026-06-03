namespace Core.Server.Packets.In.CZ;

/// <summary>
/// Pet-menu action request. rAthena <c>clif_parse_PetMenu</c> (clif.cpp, 0x01a1). Fixed 3 bytes:
/// <c>01a1 &lt;type&gt;.B</c>. Type: 0 = pet information, 1 = feed, 2 = performance, 3 = return to egg,
/// 4 = unequip accessory.
/// </summary>
public class CZ_COMMAND_PET : IncomingPacket
{
    private const int SIZE = 3;

    public byte Type { get; private set; }

    public CZ_COMMAND_PET() : base(PacketHeader.CZ_COMMAND_PET, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        Type = reader.ReadByte();
    }

    public static CZ_COMMAND_PET Create(BinaryReader reader)
    {
        var packet = new CZ_COMMAND_PET();
        packet.Read(reader);
        return packet;
    }
}
