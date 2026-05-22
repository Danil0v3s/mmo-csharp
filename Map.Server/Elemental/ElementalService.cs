using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Elemental;

/// <summary>Default <see cref="IElementalService"/>. AI lives in Mob/; this service is the rAthena-name shim.</summary>
public sealed class ElementalService : IElementalService
{
    private readonly ILogger<ElementalService> _logger;
    public ElementalService(ILogger<ElementalService> logger) => _logger = logger;

    public int Create(PlayerEntity master, int classId, int lifetimeMs) => 0;
    public int DataReceived(PlayerEntity master) => 0;
    public int Save(PlayerEntity master) => 0;
    public int Delete(PlayerEntity master) => 0;
    public int Dead(PlayerEntity master) => 0;
    public int ChangeMode(PlayerEntity master, int mode) => 0;
    public int ChangeModeAck(PlayerEntity master, int mode) => 0;
    public int CleanEffect(PlayerEntity master) => 0;
    public int Action(PlayerEntity master, EntityId targetId, long tick) => 0;
    public int SetTarget(PlayerEntity master, EntityId targetId) => 0;
    public int UnlockTarget(PlayerEntity master) => 0;
    public void Heal(PlayerEntity master, int hp, int sp) { }
    public bool SkillNotOk(PlayerEntity master, ushort skillId) => false;
    public long GetLifetimeMs(PlayerEntity master) => 0;
    public void SummonInit(PlayerEntity master) { }
    public void SummonStop(PlayerEntity master) { }

    /// <inheritdoc />
    public Core.Server.IPC.ElementalData? SerializeSnapshot(int elementalId)
    {
        // T7.3 — canonical entry point. Returning null = no live
        // elemental matches; SaveElemental skips dispatch. Wires in
        // when the per-master ElementalEntity store exposes by-id
        // lookup (same pattern as PetService.SerializeSnapshot).
        return null;
    }
}
