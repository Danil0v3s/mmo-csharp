namespace Core.Server.Packets.ClientPackets;

[PacketVersion(1)]
public class CT_AUTH : IncomingPacket
{
    private const int SIZE = 68; // header (2) + unknown (66)
    
    public byte[] Unknown { get; internal set; } // 66 bytes

    public CT_AUTH() : base(PacketHeader.CT_AUTH, SIZE)
    {
    }

    public override void Read(BinaryReader reader)
    {
        Unknown = reader.ReadBytes(66);
    }
}