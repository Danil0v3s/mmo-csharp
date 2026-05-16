namespace Core.Server.Packets;

/// <summary>
/// Pinned Ragnarok client packet version this server targets.
///
/// rAthena supports ~30 client versions stretching from 2004 to 2024; packet
/// IDs and shapes vary substantially across them. We pick one and stick to
/// it — every packet class in this project comments which version's shape it
/// implements. Changing this constant means revisiting every packet whose
/// rAthena <c>#if PACKETVER</c> branch covers the boundary.
///
/// Current pin: <b>20211103</b> — recent kRO main, has all the modern
/// char-select / map handoff packets we already emit on the char side, and
/// stable rAthena baseline behavior to mirror. Renewal only.
/// </summary>
public static class PacketVersion
{
    public const int Value = 20211103;
}
