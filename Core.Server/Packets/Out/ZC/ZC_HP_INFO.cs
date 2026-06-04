namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// The floating monster HP bar. rAthena <c>clif_monster_hp_bar</c> (clif.cpp, <c>ZC_HP_INFO</c> 0x0977).
/// Fixed 14 bytes: <c>0977 &lt;id&gt;.L &lt;hp&gt;.L &lt;maxHP&gt;.L</c>. Sent to nearby players when a mob
/// (whose HP &lt; max) takes damage or enters view, gated by <c>monster_hp_bars_info</c>.
/// </summary>
public class ZC_HP_INFO : OutgoingPacket
{
    private const int SIZE = 2 + 4 + 4 + 4; // 14

    public uint Id { get; init; }
    public int Hp { get; init; }
    public int MaxHp { get; init; }

    public ZC_HP_INFO() : base(PacketHeader.ZC_HP_INFO, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Id);
        writer.Write(Hp);
        writer.Write(MaxHp);
    }
}
