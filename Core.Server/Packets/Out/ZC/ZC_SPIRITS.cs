namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_spiritball</c> / <c>clif_servantball</c> /
/// <c>clif_abyssball</c> (clif.cpp ~3550 area). All three orb counters
/// share the same wire packet — the engine picks which counter to
/// expose via the source-side switch.
///
/// Fixed 8 bytes:
/// <code>
///   0x01d0 packet_id (2) + AID (4) + amount (2)
/// </code>
/// </summary>
public class ZC_SPIRITS : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(uint) + sizeof(short);

    public uint AccountId { get; init; }
    public short Amount { get; init; }

    public ZC_SPIRITS() : base(PacketHeader.ZC_SPIRITS, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(AccountId);
        writer.Write(Amount);
    }
}
