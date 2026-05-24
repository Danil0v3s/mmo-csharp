using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Elemental;

/// <summary>
/// Default <see cref="IElementalService"/>. AI / per-tick combat lives
/// in <c>Map.Server.Mob</c>; this service is the rAthena-named
/// lifecycle shim that owns the master ↔ elemental binding tracked on
/// <see cref="PlayerEntity.ActiveElementalClassId"/>.
/// </summary>
public sealed class ElementalService : IElementalService
{
    private readonly ILogger<ElementalService> _logger;
    public ElementalService(ILogger<ElementalService> logger) => _logger = logger;

    /// <summary>
    /// rAthena <c>elemental_create</c> — bind <paramref name="classId"/>
    /// to <paramref name="master"/> for <paramref name="lifetimeMs"/>.
    /// If the master already has an active elemental, the previous one
    /// is dropped (rAthena's elemental_delete-before-create pattern;
    /// only one elemental may be active per master).
    /// </summary>
    public int Create(PlayerEntity master, int classId, int lifetimeMs)
    {
        if (master.ActiveElementalClassId != 0)
        {
            _logger.LogDebug(
                "elemental_create: replacing class {Old} with {New} on master {Char}",
                master.ActiveElementalClassId, classId, master.CharacterId);
        }
        master.ActiveElementalClassId = classId;
        master.ActiveElementalExpiresAt = lifetimeMs > 0
            ? Environment.TickCount64 + lifetimeMs
            : 0;
        return classId;
    }

    public int DataReceived(PlayerEntity master) => 0;
    public int Save(PlayerEntity master) => 0;

    /// <summary>
    /// rAthena <c>elemental_delete</c> — clear the master's binding.
    /// Returns 1 when there was a live elemental to delete, 0
    /// otherwise.
    /// </summary>
    public int Delete(PlayerEntity master)
    {
        if (master.ActiveElementalClassId == 0) return 0;
        master.ActiveElementalClassId = 0;
        master.ActiveElementalExpiresAt = 0;
        return 1;
    }

    public int Dead(PlayerEntity master) => Delete(master);
    public int ChangeMode(PlayerEntity master, int mode) => 0;
    public int ChangeModeAck(PlayerEntity master, int mode) => 0;
    public int CleanEffect(PlayerEntity master) => 0;
    public int Action(PlayerEntity master, EntityId targetId, long tick) => 0;
    public int SetTarget(PlayerEntity master, EntityId targetId) => 0;
    public int UnlockTarget(PlayerEntity master) => 0;
    public void Heal(PlayerEntity master, int hp, int sp) { }
    public bool SkillNotOk(PlayerEntity master, ushort skillId) => false;

    /// <summary>
    /// rAthena <c>elemental_get_lifetime</c> — milliseconds remaining
    /// on the master's bound elemental, or 0 when none / expired.
    /// </summary>
    public long GetLifetimeMs(PlayerEntity master)
    {
        if (master.ActiveElementalClassId == 0) return 0;
        var remaining = master.ActiveElementalExpiresAt - Environment.TickCount64;
        return remaining > 0 ? remaining : 0;
    }

    public void SummonInit(PlayerEntity master) { }
    public void SummonStop(PlayerEntity master) => Delete(master);

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
