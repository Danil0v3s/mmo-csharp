using Map.Server.Entities;

namespace Map.Server.Status;

/// <summary>
/// rAthena <c>pc_changelook</c> / <c>clif_changelook</c> (clif.cpp:3929)
/// — broadcasts an appearance change for one slot
/// (<see cref="LookType"/>) to the AOI. Used by:
/// <list type="bullet">
///   <item>Equip/unequip — head top/mid/low + weapon + shield + shoes + robe.</item>
///   <item>NPC scripts — <c>changelook</c> opcode.</item>
///   <item>GM commands — <c>@disguise</c>, <c>@hairstyle</c>, <c>@hairc</c>.</item>
/// </list>
/// </summary>
public interface IPlayerLookService
{
    /// <summary>Broadcast <paramref name="value"/> for slot <paramref name="type"/> on this entity.</summary>
    void ChangeLook(PlayerEntity pc, LookType type, ushort value);
}
