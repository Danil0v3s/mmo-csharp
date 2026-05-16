namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Combat action — "X hit Y for N damage" (or missed, or critical, or
/// blocked). rAthena <c>clif_damage</c> with the renewal 32-bit shape
/// (<c>ZC_NOTIFY_ACT3 = 0x08c8</c>):
///
/// <code>
///   0x08c8 packet_id (2) + src AID (4) + dst AID (4) + serverTick (4) +
///   srcAmotion (4) + dstAmotion (4) + damage (4) + isSpDamage (1) +
///   div (2) + type (1) + damage2 (4) = 34 bytes
/// </code>
///
/// MS3 first slice: emitted by <see cref="IDamageService"/> on every
/// damage application; the auto-attack loop and full damage formula
/// (ATK, crit, flee, element/race modifiers) land later.
/// </summary>
public class ZC_NOTIFY_ACT3 : OutgoingPacket
{
    private const int SIZE = 34;

    public int SourceId { get; init; }
    public int TargetId { get; init; }
    public uint ServerTick { get; init; }
    public int SourceAmotion { get; init; }
    public int TargetAmotion { get; init; }
    public int Damage { get; init; }
    public byte IsSpDamage { get; init; }
    public short Div { get; init; } = 1;
    public DamageActionType ActionType { get; init; } = DamageActionType.Normal;
    public int Damage2 { get; init; }

    public ZC_NOTIFY_ACT3() : base(PacketHeader.ZC_NOTIFY_ACT3, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(SourceId);
        writer.Write(TargetId);
        writer.Write(ServerTick);
        writer.Write(SourceAmotion);
        writer.Write(TargetAmotion);
        writer.Write(Damage);
        writer.Write(IsSpDamage);
        writer.Write(Div);
        writer.Write((byte)ActionType);
        writer.Write(Damage2);
    }
}

/// <summary>
/// rAthena's damage action enum (battle.hpp <c>damage_lv</c> in part, plus
/// the visual action codes). The client interprets these to pick the right
/// hit/dodge/death animation.
/// </summary>
public enum DamageActionType : byte
{
    Normal = 0,
    PickupItem = 1,
    Sit = 2,
    Stand = 3,
    Flee = 4,
    Endure = 9,
    SplashDamage = 10,
    SkillDamage = 8,
    RepeatDamage = 11,
    MultiHit = 6,
    MultiHitCrit = 7,
    Critical = 10,
}
