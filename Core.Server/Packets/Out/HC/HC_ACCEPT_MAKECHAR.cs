namespace Core.Server.Packets.Out.HC;

public class HC_ACCEPT_MAKECHAR : OutgoingPacket
{
    public byte[] CharData { get; init; } = Array.Empty<byte>();

    public HC_ACCEPT_MAKECHAR() : base(PacketHeader.HC_ACCEPT_MAKECHAR, false) { }

    public override void Write(BinaryWriter writer)
    {
        short packetLength = (short)GetSize();
        writer.Write((short)Header);
        writer.Write(packetLength);

        // Write character data
        writer.Write(CharData);
    }

    public override int GetSize()
    {
        return sizeof(short) + sizeof(short) + CharData.Length; // packetType + packetLength + character data
    }
}