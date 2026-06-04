namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Broadcast a pet's emotion / act to everyone in view. rAthena <c>clif_pet_emotion</c> (clif.cpp,
/// 0x01aa). Fixed 10 bytes: <c>01aa &lt;GID&gt;.L &lt;data&gt;.L</c>.
/// </summary>
public class ZC_PET_ACT : OutgoingPacket
{
    private const int SIZE = 2 + 4 + 4; // 10

    public int Gid { get; init; }
    public int Data { get; init; }

    public ZC_PET_ACT() : base(PacketHeader.ZC_PET_ACT, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Gid);
        writer.Write(Data);
    }
}
