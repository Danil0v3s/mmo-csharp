namespace Core.Server.Packets.In.CH;

public class CH_DELETE_CHAR : IncomingPacket
{
    public uint CharId { get; internal set; }
    public string Email { get; internal set; } = string.Empty;

    public CH_DELETE_CHAR(int cmd) : base((PacketHeader)cmd, true) // cmd can be 0x68 or 0x1fb
    {
        Email = new string('\0', 40);
    }

    public override void Read(BinaryReader reader)
    {
        // Skip packet type (already processed)
        reader.ReadInt16(); // Skip packet type
        CharId = reader.ReadUInt32();

        // Read email (40 bytes)
        Email = reader.ReadFixedString(40);
    }

    public override int GetSize()
    {
        int cmd = (int)Header;
        int size = sizeof(short) + sizeof(uint) + 40; // packetType + charId + email[40]
        if (cmd == 0x1fb)
        {
            size += 10; // Additional bytes for newer packet
        }
        return cmd == 0x68 ? 46 : 56; // Fixed sizes based on packet type
    }
}