namespace Core.Server.Packets.In.CZ;

/// <summary>
/// "What's that entity's name?" rAthena <c>clif_parse_SolveCharName</c>.
/// Fixed 6 bytes: 0x0368 packet_id (2) + entityId (4).
///
/// Clients send this on hover, click, and other casual lookups to fill in
/// the floating name above a player/NPC/mob. Server replies with one of:
/// <see cref="Out.ZC.ZC_ACK_REQNAMEALL"/> (PC), <c>ZC_ACK_REQNAMEALL_NPC</c>
/// (NPC), or the mob equivalent — keyed by the entity's block-list type.
///
/// The packet ID is the PACKETVER ≥ 20180307 shuffled value (rAthena
/// <c>clif_shuffle.hpp</c>); pre-shuffle this packet was 0x0094.
/// </summary>
public class CZ_REQNAME : IncomingPacket
{
    private const int SIZE = 6;

    public uint EntityId { get; private set; }

    public CZ_REQNAME() : base(PacketHeader.CZ_REQNAME, SIZE) { }

    public override void Read(BinaryReader reader)
    {
        EntityId = reader.ReadUInt32();
    }

    public static CZ_REQNAME Create(BinaryReader reader)
    {
        var packet = new CZ_REQNAME();
        packet.Read(reader);
        return packet;
    }
}
