using Core.Server.Packets;

namespace Core.Server.Packets.ClientPackets;

[PacketVersion(1)]
public class CT_AUTH : IncomingPacket
{
    public byte[] Unknown { get; internal set; } // 66 bytes

    public CT_AUTH() : base(PacketHeader.CT_AUTH, isFixedLength: true)
    {
    }

    public override void Read(BinaryReader reader)
    {
        Unknown = reader.ReadBytes(66);
    }
    
    public override int GetSize()
    {
        // header (2) + unknown (66)
        return 2 + 66;
    }
}