namespace Core.Server.Packets.In;

/// <summary>
/// Internal health-check ping from the test harness. Empty body — just the
/// 2-byte header. Servers respond with <see cref="Out.ZC_INTERNAL_PONG"/>;
/// the Ready flag in the response signals whether the server has finished
/// booting (DB loads, peer registrations, etc.) and is safe to drive with
/// real client packets.
/// </summary>
public class CZ_INTERNAL_PING : IncomingPacket
{
    private const int SIZE = 2; // header only

    public CZ_INTERNAL_PING() : base(PacketHeader.CZ_INTERNAL_PING, SIZE) { }

    public override void Read(BinaryReader reader)
    {
    }
}
