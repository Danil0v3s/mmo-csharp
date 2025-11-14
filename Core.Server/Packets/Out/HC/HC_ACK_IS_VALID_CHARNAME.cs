namespace Core.Server.Packets.Out.HC;

public class HC_ACK_IS_VALID_CHARNAME : OutgoingPacket
{
    public ushort Result { get; init; }

    public HC_ACK_IS_VALID_CHARNAME() : base(PacketHeader.HC_ACK_IS_VALID_CHARNAME, true) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);
        writer.Write(Result);
    }

    public override int GetSize()
    {
        return sizeof(short) + sizeof(ushort); // packetType + result
    }
}