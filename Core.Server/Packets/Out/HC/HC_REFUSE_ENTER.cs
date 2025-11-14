namespace Core.Server.Packets.Out.HC;

public class HC_REFUSE_ENTER : OutgoingPacket
{
    public byte ErrorCode { get; init; }

    public HC_REFUSE_ENTER() : base(PacketHeader.HC_REFUSE_ENTER, true) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);
        writer.Write(ErrorCode);
    }

    public override int GetSize()
    {
        return sizeof(short) + sizeof(byte); // packetType + errorCode
    }
}