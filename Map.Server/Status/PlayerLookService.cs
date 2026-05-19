using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Visibility;

namespace Map.Server.Status;

/// <summary>
/// Default <see cref="IPlayerLookService"/> — emits
/// <c>ZC_SPRITE_CHANGE2</c> (0x01D7, the post-20181121 packet variant
/// used by PACKETVER 20220401) to every viewer in the AOI.
///
/// The legacy <c>ZC_SPRITE_CHANGE</c> (0x01D1, byte val) and
/// <c>ZC_SPRITE_CHANGE3</c> intermediates aren't shipped — modern
/// clients ignore them.
/// </summary>
public sealed class PlayerLookService : IPlayerLookService
{
    private readonly IVisibilityService _visibility;

    public PlayerLookService(IVisibilityService visibility)
    {
        _visibility = visibility;
    }

    public void ChangeLook(PlayerEntity pc, LookType type, ushort value)
    {
        // rAthena clif_changelook: AREA broadcast (every viewer in
        // sight of bl). The packet body matches the modern look-type
        // encoding — Value2 is reserved for the weapon+shield doublet.
        _visibility.SendToArea(pc, new ZC_SPRITE_CHANGE2
        {
            AccountId = (uint)pc.AccountId,
            LookType = (byte)type,
            Value = value,
            Value2 = 0,
        });
    }
}
