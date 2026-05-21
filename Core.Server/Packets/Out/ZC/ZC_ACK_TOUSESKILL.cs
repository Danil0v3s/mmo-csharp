namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_skill_fail</c> ([clif.cpp ~3450]) — sent only to the
/// casting player when their skill attempt is rejected (gated by SP,
/// requirement check, range, cooldown, immune target, etc.).
///
/// Modern renewal wire format (PACKETVER ≥ 20181121, id <c>0x0110</c>,
/// 14 bytes fixed):
/// <code>
///   0x0110 (2) + skillId (2) + btype (4) + itemId (4) + flag (1) + cause (1)
/// </code>
///
/// <list type="bullet">
///   <item><c>btype</c>: skill type context. For most fails it is 0;
///         certain causes (item-skill rejection) use it to surface the
///         item amount that was insufficient.</item>
///   <item><c>itemId</c>: optional item id the client mentions in the
///         fail message (e.g. "You need 1 Red Gemstone").</item>
///   <item><c>flag</c>: always 0 (= failed). rAthena reserves 1 for a
///         theoretical "succeeded" frame that is never emitted on this
///         packet — keep at 0.</item>
///   <item><c>cause</c>: <see cref="SkillFailCause"/> enum value the
///         client renders into a localized fail string.</item>
/// </list>
/// </summary>
public class ZC_ACK_TOUSESKILL : OutgoingPacket
{
    private const int SIZE = 14;

    public ushort SkillId { get; init; }
    public int Btype { get; init; }
    public uint ItemId { get; init; }
    public byte Flag { get; init; } = 0;
    public byte Cause { get; init; }

    public ZC_ACK_TOUSESKILL() : base(PacketHeader.ZC_ACK_TOUSESKILL, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(SkillId);
        writer.Write(Btype);
        writer.Write(ItemId);
        writer.Write(Flag);
        writer.Write(Cause);
    }
}

/// <summary>
/// rAthena <c>useskill_fail_cause</c> (status.hpp) — the byte the client
/// maps to a localized "skill fail" string. Only the most common values
/// are enumerated here; rAthena defines ~75 total. Add more on demand
/// when a port surfaces a fail mode the existing entries don't cover.
/// </summary>
public enum SkillFailCause : byte
{
    SkillFail = 0,            // generic "skill failed"
    SpInsufficient = 1,       // not enough SP
    HpInsufficient = 2,       // not enough HP
    NoMemo = 3,               // no memo'd warp point
    Delay = 4,                // skill delay still active
    ZenyInsufficient = 5,     // not enough zeny
    WrongWeapon = 6,          // wrong weapon equipped
    NoRedJewel = 7,           // red gemstone required
    NoBlueJewel = 8,          // blue gemstone required
    Weight = 9,               // overweight
    NoCombo = 10,             // combo prerequisite not met
    StealCoin = 11,           // mug failed (no coin)
    NoEnemy = 12,             // no enemy in range
    NoSpiritualSphere = 13,   // missing spirit sphere
    UndeadId = 14,            // target is undead
    Stuff = 15,               // need ammo / projectile
    State = 16,               // wrong state (e.g. not in cart)
    Skill = 17,               // need other skill first
    Item = 18,                // need item
    InvokerNotConfirm = 19,
    NeedHelpers = 20,
    NeedEquipment = 21,
    NeedMoreBullet = 22,
    Coin = 23,
    Amount = 24,
    Sight = 25,
    SummonNone = 26,
    NeedItem = 27,
}
