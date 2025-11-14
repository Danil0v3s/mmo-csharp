namespace Core.Server.Packets.Out.HC;

public class HC_CHAR_DELETE2_ACCEPT_ACK : OutgoingPacket
{
    public uint CharId { get; init; }
    public uint Result { get; init; }

    public HC_CHAR_DELETE2_ACCEPT_ACK() : base(PacketHeader.HC_CHAR_DELETE2_ACCEPT_ACK, true) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);
        writer.Write(CharId);
        writer.Write(Result);
    }

    public override int GetSize()
    {
        return sizeof(short) + sizeof(uint) + sizeof(uint); // packetType + charId + result
    }
}