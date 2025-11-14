namespace Core.Server.Packets.Out.HC;

public class HC_ACK_CHARINFO_PER_PAGE : OutgoingPacket
{
    public byte[] CharInfoData { get; init; } = Array.Empty<byte>();
    public byte Count { get; init; }

    public HC_ACK_CHARINFO_PER_PAGE() : base(PacketHeader.HC_ACK_CHARINFO_PER_PAGE, -1) { }

    public override void Write(BinaryWriter writer)
    {
        short packetLength = (short)GetSize();
        writer.Write((short)Header);
        writer.Write(packetLength);

        // Write character info data
        writer.Write(CharInfoData);
    }

    public override int GetSize()
    {
        return sizeof(short) + sizeof(short) + CharInfoData.Length; // packetType + packetLength + data
    }
}