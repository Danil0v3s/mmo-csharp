using Core.Server.Packets;

namespace Core.Server.Packets.ServerPackets;

[PacketVersion(1)]
public class SC_NOTIFY_BAN : OutgoingPacket
{
    public byte Result { get; init; }

    public SC_NOTIFY_BAN() : base(PacketHeader.SC_NOTIFY_BAN, isFixedLength: true)
    {
    }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);
        writer.Write(Result);
    }

    public override int GetSize()
    {
        // Header (2) + result (1)
        return 2 + 1;
    }
}