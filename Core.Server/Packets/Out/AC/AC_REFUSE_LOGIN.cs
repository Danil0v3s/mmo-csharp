using System.Text;

namespace Core.Server.Packets.Out.AC;

[PacketVersion(1)]
public class AC_REFUSE_LOGIN : OutgoingPacket
{
    public uint Error { get; init; }
    public string UnblockTime { get; init; }

    public AC_REFUSE_LOGIN() : base(PacketHeader.AC_REFUSE_LOGIN, isFixedLength: true)
    {
    }

    public override void Write(BinaryWriter writer)
    {
        writer.Write((short)Header);
        writer.Write(Error); // For version >= 20120000
        writer.Write(Encoding.UTF8.GetBytes(UnblockTime.PadRight(20, '\0')));
    }

    public override int GetSize()
    {
        // Header (2) + error (4) + unblock_time (20)
        return 2 + 4 + 20;
    }
}