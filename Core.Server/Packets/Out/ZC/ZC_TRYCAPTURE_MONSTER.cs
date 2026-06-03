namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Taming result roulette. rAthena <c>clif_pet_roulette</c> (clif.cpp, 0x01a0). Fixed 3 bytes:
/// <c>01a0 &lt;result&gt;.B</c> — 1 = the monster was caught (egg incoming), 0 = the attempt failed.
/// </summary>
public class ZC_TRYCAPTURE_MONSTER : OutgoingPacket
{
    private const int SIZE = 3;

    public bool Success { get; init; }

    public ZC_TRYCAPTURE_MONSTER() : base(PacketHeader.ZC_TRYCAPTURE_MONSTER, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Success ? (byte)1 : (byte)0);
    }
}
