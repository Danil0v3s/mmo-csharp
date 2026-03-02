namespace Core.Server.Packets.Out.HC;

public class HC_ACCEPT_DELETECHAR : OutgoingPacket
{
    private const int SIZE = 2; // packetType only

    public HC_ACCEPT_DELETECHAR() : base(PacketHeader.HC_ACCEPT_DELETECHAR, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);
    }
}
