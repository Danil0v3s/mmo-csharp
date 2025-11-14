namespace Core.Server.Packets.Out.HC;

public class HC_CHAR_DELETE2_ACK : OutgoingPacket
{
    public uint CharId { get; init; }
    public uint Result { get; init; }
    public uint DeleteDate { get; init; }

    public HC_CHAR_DELETE2_ACK() : base(PacketHeader.HC_CHAR_DELETE2_ACK, true) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);
        writer.Write(CharId);
        writer.Write(Result);
        writer.Write(DeleteDate);
    }

    public override int GetSize()
    {
        return sizeof(short) + sizeof(uint) + sizeof(uint) + sizeof(uint); // packetType + charId + result + deleteDate
    }
}