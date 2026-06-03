namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// Party member HP-bar update. rAthena <c>clif_party_hp</c> (clif.cpp) — the classic 0x0106 form.
/// Fixed 10 bytes: <c>0106 &lt;AID&gt;.L &lt;hp&gt;.W &lt;maxhp&gt;.W</c>. When max HP exceeds the 16-bit field,
/// rAthena scales to a percentage (hp = hp/(maxhp/100), maxhp = 100) so the bar still renders.
/// Broadcast to same-map party members (PARTY_AREA_WOS).
/// </summary>
public class ZC_NOTIFY_HP_TO_GROUPM : OutgoingPacket
{
    private const int SIZE = sizeof(short) + sizeof(uint) + sizeof(short) + sizeof(short); // 10

    public uint AccountId { get; init; }
    public int Hp { get; init; }
    public int MaxHp { get; init; }

    public ZC_NOTIFY_HP_TO_GROUPM() : base(PacketHeader.ZC_NOTIFY_HP_TO_GROUPM, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        int hp = Hp, maxhp = MaxHp;
        if (maxhp > short.MaxValue && maxhp > 0)
        {
            hp = hp / Math.Max(1, maxhp / 100); // percentage so the 16-bit bar still renders
            maxhp = 100;
        }
        writer.Write(AccountId);
        writer.Write((short)Math.Clamp(hp, 0, short.MaxValue));
        writer.Write((short)Math.Clamp(maxhp, 0, short.MaxValue));
    }
}
