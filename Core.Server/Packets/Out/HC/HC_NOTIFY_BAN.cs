namespace Core.Server.Packets.Out.HC;

public class HC_NOTIFY_BAN : OutgoingPacket
{
    public byte Result { get; init; }

    public HC_NOTIFY_BAN() : base(PacketHeader.SC_NOTIFY_BAN, true) { } // Using SC_NOTIFY_BAN (0x81) as it's the same packet

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);
        writer.Write(Result);
    }

    public override int GetSize()
    {
        return sizeof(short) + sizeof(byte); // packetType + result
    }
}