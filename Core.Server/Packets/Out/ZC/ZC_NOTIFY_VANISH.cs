namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// "Entity vanished from your view." rAthena <c>clif_clearunit_single</c>.
/// Shape: 0x0080 packet_id (2) + entityId (4) + reason (1) = 7 bytes.
///
/// Reason codes (rAthena clif.hpp <c>enum clr_type</c>):
///   0 = OUTSIGHT (left view range)
///   1 = DIED
///   2 = LOGOUT (disconnect)
///   3 = TELEPORT (warp scroll, etc.)
///   4 = TRICKDEAD (status effect)
/// </summary>
public class ZC_NOTIFY_VANISH : OutgoingPacket
{
    private const int SIZE = 7;

    public int EntityId { get; init; }
    public VanishReason Reason { get; init; }

    public ZC_NOTIFY_VANISH() : base(PacketHeader.ZC_NOTIFY_VANISH, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(EntityId);
        writer.Write((byte)Reason);
    }
}

public enum VanishReason : byte
{
    Outsight = 0,
    Died = 1,
    Logout = 2,
    Teleport = 3,
    TrickDead = 4,
}
