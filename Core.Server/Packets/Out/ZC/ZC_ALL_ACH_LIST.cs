namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_achievement_list_all</c> ([clif.cpp:21773]). The full
/// achievement progress summary. Variable length; for a fresh character
/// only the header + summary fields are emitted (no per-achievement
/// entries). Real implementation lands with the achievement system.
/// </summary>
public class ZC_ALL_ACH_LIST : OutgoingPacket
{
    public byte[] Body { get; init; } = Array.Empty<byte>();

    public ZC_ALL_ACH_LIST() : base(PacketHeader.ZC_ALL_ACH_LIST, -1) { }

    public override int GetSize() => sizeof(short) + sizeof(short) + Body.Length;

    public override void Write(BinaryWriter writer) => writer.Write(Body);
}
