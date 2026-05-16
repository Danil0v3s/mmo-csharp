namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// "An entity is standing here." Sent on initial view (player just entered range)
/// and on spawn. rAthena <c>clif_set_unit_idle</c> with the modern packet shape
/// (PACKETVER >= 20150513 → 0x09ff, "idle_unit"). Variable length; the wire
/// format follows rAthena's <c>struct packet_idle_unit</c> exactly so existing
/// game clients accept it.
///
/// MS1 scope: emit a valid skeleton with the IDs + position + sex set, and
/// every cosmetic field zeroed. That's enough for a player to render at the
/// right cell. Full cosmetic fidelity (head/weapon/equip ids, status icons,
/// guild emblem) comes when [items.md](../../adjacent/items.md) and
/// [status.md](../../adjacent/status.md) land.
/// </summary>
public class ZC_NOTIFY_STANDENTRY : OutgoingPacket
{
    // Fixed prefix size for the chosen PACKETVER (20211103):
    //   packetType(2) + packetLength(2) + objecttype(1) + AID(4) + GID(4) +
    //   speed(2) + bodyState(2) + healthState(2) + effectState(4) +
    //   job(2) + head(2) + weapon(4) + shield(4) + accessory(2) +
    //   accessory2(2) + accessory3(2) + headpalette(2) + bodypalette(2) +
    //   headDir(2) + robe(2) + GUID(4) + GEmblemVer(2) + honor(2) +
    //   virtue(4) + isPKModeON(1) + sex(1) + PosDir(3) + xSize(1) + ySize(1) +
    //   state(1) + clevel(2) + font(2) + maxHP(4) + HP(4) + isBoss(1) +
    //   body(2) + name(24) = 108 bytes total
    private const int PacketLength = 108;
    private const int NameLength = 24;

    public byte ObjectType { get; init; }       // 0=PC, 1=NPC, 5=MOB etc. (clif_bl_type)
    public int AccountId { get; init; }
    public int CharacterOrEntityId { get; init; }
    public short Speed { get; init; } = 150;
    public short Job { get; init; }             // sprite class
    public byte Sex { get; init; }
    public short X { get; init; }
    public short Y { get; init; }
    public byte Dir { get; init; }
    public short ClientLevel { get; init; } = 1;
    public string Name { get; init; } = string.Empty;

    public ZC_NOTIFY_STANDENTRY() : base(PacketHeader.ZC_NOTIFY_STANDENTRY, PacketLength) { }

    public override bool HasPacketLength => true;
    public override int GetSize() => PacketLength;

    public override void Write(BinaryWriter writer)
    {
        // From here we emit body bytes only — the base WritePacket has already
        // written packetType + packetLength.
        writer.Write(ObjectType);                 // 1
        writer.Write(AccountId);                  // 4
        writer.Write(CharacterOrEntityId);        // 4
        writer.Write(Speed);                      // 2
        writer.Write((short)0);                   // bodyState
        writer.Write((short)0);                   // healthState
        writer.Write((int)0);                     // effectState
        writer.Write(Job);                        // 2
        writer.Write((short)0);                   // head
        writer.Write((int)0);                     // weapon
        writer.Write((int)0);                     // shield
        writer.Write((short)0);                   // accessory
        writer.Write((short)0);                   // accessory2
        writer.Write((short)0);                   // accessory3
        writer.Write((short)0);                   // headpalette
        writer.Write((short)0);                   // bodypalette
        writer.Write((short)0);                   // headDir
        writer.Write((short)0);                   // robe
        writer.Write((int)0);                     // GUID
        writer.Write((short)0);                   // GEmblemVer
        writer.Write((short)0);                   // honor
        writer.Write((int)0);                     // virtue
        writer.Write((byte)0);                    // isPKModeON
        writer.Write(Sex);                        // 1
        PositionPacker.WritePos(writer, X, Y, Dir); // 3
        writer.Write((byte)5);                    // xSize (ignored)
        writer.Write((byte)5);                    // ySize (ignored)
        writer.Write((byte)0);                    // state (dead/sit)
        writer.Write(ClientLevel);                // 2
        writer.Write((short)0);                   // font
        writer.Write((int)0);                     // maxHP (MS3 will populate)
        writer.Write((int)0);                     // HP
        writer.Write((byte)0);                    // isBoss
        writer.Write((short)0);                   // body
        var nameBytes = new byte[NameLength];
        var src = System.Text.Encoding.ASCII.GetBytes(Name ?? string.Empty);
        Array.Copy(src, nameBytes, Math.Min(src.Length, NameLength - 1));
        writer.Write(nameBytes);                  // 24
    }
}
