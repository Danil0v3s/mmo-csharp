namespace Core.Server.Packets.In.CZ;

/// <summary>
/// "Send me back to char-select" / "Respawn me". rAthena
/// <c>clif_parse_Restart</c>. Fixed 3 bytes: 0x00b2 packet_id (2) + type (1).
///
/// <list type="bullet">
///   <item><see cref="Type"/> = 0 — respawn (after death). Server triggers
///     <c>pc_respawn</c> equivalent (heal + move to save point).</item>
///   <item><see cref="Type"/> = 1 — back to char-select. Server saves the
///     player and acks with <c>ZC_RESTART_ACK</c> type 1; the client then
///     reconnects to the char-server's character list.</item>
/// </list>
/// </summary>
public class CZ_RESTART : IncomingPacket
{
    private const int SIZE = 3;

    public byte Type { get; private set; }

    public CZ_RESTART() : base(PacketHeader.CZ_RESTART, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        Type = reader.ReadByte();
    }

    public static CZ_RESTART Create(BinaryReader reader)
    {
        var packet = new CZ_RESTART();
        packet.Read(reader);
        return packet;
    }
}
