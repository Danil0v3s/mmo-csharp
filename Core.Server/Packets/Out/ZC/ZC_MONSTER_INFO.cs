namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_skill_estimation</c> (clif.cpp ~5980) — emits the
/// Sense / Estimation result frame after a Mage's Sense or Hunter's
/// Estimation skill resolves. Fixed 29 bytes:
/// <code>
///   0x018c (2) + class (2) + level (2) + size (2) + hp (4) + def (2)
///         + race (2) + mdef (2) + element (2) + ele_lv (1) + dmg_water (1)
///         + dmg_earth (1) + dmg_fire (1) + dmg_wind (1) + dmg_poison (1)
///         + dmg_holy (1) + dmg_shadow (1) + dmg_ghost (1) + dmg_undead (1)
/// </code>
/// Renders the mob-info popup on the client (HP bar + per-element
/// damage modifiers + race + size).
/// </summary>
public class ZC_MONSTER_INFO : OutgoingPacket
{
    private const int SIZE = 29;

    public ushort ClassId { get; init; }
    public ushort Level { get; init; }
    public ushort Size { get; init; }
    public int Hp { get; init; }
    public ushort Def { get; init; }
    public ushort Race { get; init; }
    public ushort Mdef { get; init; }
    public ushort Element { get; init; }
    public byte ElementLv { get; init; }
    /// <summary>rAthena: per-element damage modifier table (water/earth/fire/wind/poison/holy/shadow/ghost/undead) — signed % deltas.</summary>
    public sbyte DmgWater { get; init; }
    public sbyte DmgEarth { get; init; }
    public sbyte DmgFire { get; init; }
    public sbyte DmgWind { get; init; }
    public sbyte DmgPoison { get; init; }
    public sbyte DmgHoly { get; init; }
    public sbyte DmgShadow { get; init; }
    public sbyte DmgGhost { get; init; }
    public sbyte DmgUndead { get; init; }

    public ZC_MONSTER_INFO() : base(PacketHeader.ZC_MONSTER_INFO, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(ClassId);
        writer.Write(Level);
        writer.Write(Size);
        writer.Write(Hp);
        writer.Write(Def);
        writer.Write(Race);
        writer.Write(Mdef);
        writer.Write(Element);
        writer.Write(ElementLv);
        writer.Write(DmgWater);
        writer.Write(DmgEarth);
        writer.Write(DmgFire);
        writer.Write(DmgWind);
        writer.Write(DmgPoison);
        writer.Write(DmgHoly);
        writer.Write(DmgShadow);
        writer.Write(DmgGhost);
        writer.Write(DmgUndead);
    }
}
