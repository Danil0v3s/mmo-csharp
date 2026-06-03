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
/// rAthena <c>e_useskill_fail_cause</c> (clif.hpp:402) — the byte the client maps to a
/// localized "skill fail" string. It is sent <b>raw</b> on the wire (<c>clif_skill_fail</c> →
/// <c>ZC_ACK_TOUSESKILL.Cause = (byte)cause</c>), so every value MUST equal rAthena's for the
/// client to render the correct message.
///
/// <para>COMBAT-93 — reconciled the whole enum to <c>e_useskill_fail_cause</c>. The prior values
/// were a legacy partial renumbering that mostly did NOT match (e.g. <c>SummonNone</c> was 26 vs
/// rAthena 20, <c>Skill</c> 17 vs 16, <c>NeedHelpers</c> 20 vs 17). C# member names are kept (so
/// call sites stay source-compatible); the rAthena cause each maps to is noted inline. A few
/// C#-invented causes have no <c>e_useskill_fail_cause</c> equivalent — they fall back to the
/// generic <c>USESKILL_FAIL_LEVEL = 0</c> ("skill failed"), the correct client behavior for an
/// unmapped cause. Add more rAthena causes on demand.</para>
/// </summary>
public enum SkillFailCause : byte
{
    SkillFail = 0,            // USESKILL_FAIL_LEVEL (generic "skill failed")
    SpInsufficient = 1,       // USESKILL_FAIL_SP_INSUFFICIENT
    HpInsufficient = 2,       // USESKILL_FAIL_HP_INSUFFICIENT
    Stuff = 3,                // USESKILL_FAIL_STUFF_INSUFFICIENT (need ammo / projectile)
    Delay = 4,                // USESKILL_FAIL_SKILLINTERVAL
    ZenyInsufficient = 5,     // USESKILL_FAIL_MONEY
    WrongWeapon = 6,          // USESKILL_FAIL_THIS_WEAPON
    NoRedJewel = 7,           // USESKILL_FAIL_REDJAMSTONE
    NoBlueJewel = 8,          // USESKILL_FAIL_BLUEJAMSTONE
    Weight = 9,               // USESKILL_FAIL_WEIGHTOVER
    NoEnemy = 11,             // USESKILL_FAIL_TOTARGET (no target)
    Skill = 16,               // USESKILL_FAIL_NEED_OTHER_SKILL (need other skill first)
    NeedHelpers = 17,         // USESKILL_FAIL_NEED_HELPER
    SummonNone = 20,          // USESKILL_FAIL_SUMMON_NONE
    NeedEquipmentKunai = 34,  // USESKILL_FAIL_NEED_EQUIPMENT_KUNAI
    State = 57,               // USESKILL_FAIL_CART (need to be in a cart / mounted state)
    NeedItem = 71,            // USESKILL_FAIL_NEED_ITEM
    Item = 71,                // USESKILL_FAIL_NEED_ITEM (alias of NeedItem)
    NeedEquipment = 72,       // USESKILL_FAIL_NEED_EQUIPMENT
    NoCombo = 73,             // USESKILL_FAIL_COMBOSKILL
    NoSpiritualSphere = 74,   // USESKILL_FAIL_SPIRITS
    NeedMoreBullet = 84,      // USESKILL_FAIL_NEED_MORE_BULLET
    Coin = 85,                // USESKILL_FAIL_COINS

    // No exact e_useskill_fail_cause equivalent — fall back to the generic LEVEL = 0 ("skill
    // failed") so the client renders the generic string. Unused today; kept for source compat.
    NoMemo = 0,
    StealCoin = 0,
    UndeadId = 0,
    InvokerNotConfirm = 0,
    Amount = 0,
    Sight = 0,
}
