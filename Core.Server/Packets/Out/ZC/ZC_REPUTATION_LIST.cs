namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_reputation_list</c> ([clif.cpp:11020]). Reputation
/// progress per faction. Variable length; empty for a fresh character.
/// </summary>
public class ZC_REPUTATION_LIST : OutgoingPacket
{
    public byte[] Body { get; init; } = Array.Empty<byte>();

    public ZC_REPUTATION_LIST() : base(PacketHeader.ZC_REPUTATION_LIST, -1) { }

    public override int GetSize() => sizeof(short) + sizeof(short) + Body.Length;

    public override void Write(BinaryWriter writer) => writer.Write(Body);
}
