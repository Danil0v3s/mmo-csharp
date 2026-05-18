namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// "Yes, you may return to char-select / respawn." rAthena
/// <c>clif_charselectok</c>. Fixed 3 bytes: 0x00b3 packet_id (2) + type (1).
///
/// <list type="bullet">
///   <item><see cref="Type"/> = 1 — disconnect + go to char-select.</item>
///   <item>Other values — client treats as a no-op / refused.</item>
/// </list>
/// </summary>
public class ZC_RESTART_ACK : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(byte);

    public byte Type { get; init; }

    public ZC_RESTART_ACK() : base(PacketHeader.ZC_RESTART_ACK, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Type);
    }
}
