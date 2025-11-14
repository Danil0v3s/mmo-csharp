namespace Core.Server.Packets.In.CH;

public class CH_DELETE_CHAR : IncomingPacket
{
    public uint CharId { get; internal set; }
    public string Email { get; internal set; } = string.Empty;

    public CH_DELETE_CHAR(int cmd) : base((PacketHeader)cmd, cmd == 0x68 ? 46 : 56) // cmd can be 0x68 (46 bytes) or 0x1fb (56 bytes)
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
}