namespace Core.Server.Packets.Out.ZC;

/// <summary>
/// rAthena <c>clif_arrow_fail</c> (clif.cpp:4217) — <c>PACKET_ZC_ACTION_FAILURE</c>
/// (id <c>0x013b</c>, 4 bytes fixed): <c>0x013b (2) + type (2)</c>. Sent only to the
/// caster when a ranged action can't proceed because no/wrong projectile is equipped.
///
/// <para>The <c>type</c> is <see cref="ArrowFailType"/> — the renewal "equip arrows"
/// message family. rAthena uses <see cref="ArrowFailType.NoAmmo"/> for both the
/// no-ammo-equipped and wrong-ammo-type cases (skill.cpp:19614/19635).</para>
/// </summary>
public class ZC_ACTION_FAILURE : OutgoingPacket
{
    private const int SIZE = 4;

    public ushort Type { get; init; }

    public ZC_ACTION_FAILURE() : base(PacketHeader.ZC_ACTION_FAILURE, SIZE) { }

    public override void Write(BinaryWriter writer)
    {
        writer.Write(Type);
    }
}

/// <summary>rAthena <c>e_action_failure</c> (clif.hpp:793).</summary>
public enum ArrowFailType : ushort
{
    NoAmmo = 0,        // ARROWFAIL_NO_AMMO — "You don't have arrows."
    WeightLimit = 1,   // ARROWFAIL_WEIGHT_LIMIT
    WeightLimit2 = 2,  // ARROWFAIL_WEIGHT_LIMIT2
    Success = 3,       // ARROWFAIL_SUCCESS
}
