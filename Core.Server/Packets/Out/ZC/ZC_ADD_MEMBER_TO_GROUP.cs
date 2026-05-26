namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// New-member-joined broadcast to existing party members. rAthena
/// <c>clif_party_member_info</c> (clif.cpp:7812). PACKETVER &gt;= 20171207
/// variant <c>0x0ae4</c> (<c>partymemberinfo</c>) with GID + class +
/// baseLevel fields.
/// <para>Wire layout (89 bytes):</para>
/// <code>
/// 0x0ae4 (2) + AID (4) + GID (4) + leader (4)
///        + class (2) + baseLevel (2)
///        + x (2) + y (2)
///        + offline (1)
///        + partyName (24) + playerName (24) + mapName (16)
///        + sharePickup (1) + shareLoot (1)
/// </code>
/// <para>rAthena clif.cpp:7806-7811: <c>leader=0</c> means leader,
/// <c>leader=1</c> means normal. <c>offline=0</c> means connected,
/// <c>offline=1</c> means disconnected.</para>
/// </summary>
public class ZC_ADD_MEMBER_TO_GROUP : OutgoingPacket
{
    private const int NameLength = 24;
    private const int MapNameLength = 16;
    private const int SIZE = sizeof(short)
                           + sizeof(uint)   // AID
                           + sizeof(uint)   // GID (char id)
                           + sizeof(uint)   // leader (0=leader, 1=member)
                           + sizeof(short)  // class
                           + sizeof(short)  // baseLevel
                           + sizeof(short)  // x
                           + sizeof(short)  // y
                           + sizeof(byte)   // offline
                           + NameLength
                           + NameLength
                           + MapNameLength
                           + sizeof(byte)   // sharePickup
                           + sizeof(byte);  // shareLoot

    public uint AccountId { get; init; }
    public uint CharacterId { get; init; }
    /// <summary>0 = leader, 1 = normal member (rAthena clif.cpp:7826 inversion).</summary>
    public uint Leader { get; init; }
    public short ClassId { get; init; }
    public short BaseLevel { get; init; }
    public short X { get; init; }
    public short Y { get; init; }
    /// <summary>0 = connected, 1 = disconnected (rAthena clif.cpp:7833 inversion).</summary>
    public byte Offline { get; init; }
    public string PartyName { get; init; } = string.Empty;
    public string PlayerName { get; init; } = string.Empty;
    public string MapName { get; init; } = string.Empty;
    /// <summary><c>party.item &amp; 1</c> → 1 = item pickup share on.</summary>
    public byte SharePickup { get; init; }
    /// <summary><c>party.item &amp; 2</c> → 1 = MVP / loot share on.</summary>
    public byte ShareLoot { get; init; }

    public ZC_ADD_MEMBER_TO_GROUP() : base(PacketHeader.ZC_ADD_MEMBER_TO_GROUP, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(AccountId);
        writer.Write(CharacterId);
        writer.Write(Leader);
        writer.Write(ClassId);
        writer.Write(BaseLevel);
        writer.Write(X);
        writer.Write(Y);
        writer.Write(Offline);
        writer.WriteFixedString(PartyName ?? string.Empty, NameLength);
        writer.WriteFixedString(PlayerName ?? string.Empty, NameLength);
        writer.WriteFixedString(MapName ?? string.Empty, MapNameLength);
        writer.Write(SharePickup);
        writer.Write(ShareLoot);
    }
}
