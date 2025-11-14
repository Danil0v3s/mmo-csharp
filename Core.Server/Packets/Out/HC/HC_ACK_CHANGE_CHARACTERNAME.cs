namespace Core.Server.Packets.Out.HC;

public class HC_ACK_CHANGE_CHARACTERNAME : OutgoingPacket
{
    public uint Result { get; init; }

    public HC_ACK_CHANGE_CHARACTERNAME() : base(PacketHeader.HC_ACK_CHANGE_CHARACTERNAME, true) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);
        writer.Write(Result);
    }

    public override int GetSize()
    {
        return sizeof(short) + sizeof(uint); // packetType + result
    }
}