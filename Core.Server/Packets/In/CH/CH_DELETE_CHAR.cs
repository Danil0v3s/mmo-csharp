namespace Core.Server.Packets.In.CH;

public class CH_DELETE_CHAR : IncomingPacket
{
    public uint CharId { get; internal set; }
    public string Key { get; internal set; } = string.Empty;

    private const int SIZE = 56; // packetType (2) + charId (4) + key (50)

    public CH_DELETE_CHAR() : base(PacketHeader.CH_DELETE_CHAR, SIZE)
    {
        Key = new string('\0', 50);
    }

    public override void Read(BinaryReader reader)
    {
        CharId = reader.ReadUInt32();

        // Read delete key (50 bytes)
        Key = reader.ReadFixedString(50);
    }
}
