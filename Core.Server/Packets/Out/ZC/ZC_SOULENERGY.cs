namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_soulball</c> (clif.cpp:3622) — Soul Reaper soul
/// counter. Same wire shape as <see cref="ZC_SPIRITS"/> but a separate
/// header so the modern client can render the soul orb art instead of
/// the white spheres.
///
/// Fixed 8 bytes:
/// <code>
///   0x0b73 packet_id (2) + AID (4) + amount (2)
/// </code>
/// </summary>
public class ZC_SOULENERGY : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(uint) + sizeof(short);

    public uint AccountId { get; init; }
    public short Amount { get; init; }

    public ZC_SOULENERGY() : base(PacketHeader.ZC_SOULENERGY, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(AccountId);
        writer.Write(Amount);
    }
}
