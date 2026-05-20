using Map.Server.Entities;

namespace Map.Server.Elemental;

/// <summary>
/// Sorcerer Elemental companion. Canonical entry points for rAthena
/// <c>elemental.cpp</c> (1 149 lines, 19 functions). The C# port
/// already has Elemental AI in <c>Map.Server.Mob</c>; this service
/// owns the rAthena-named lifecycle hooks.
/// </summary>
public interface IElementalService
{
    /// <summary>rAthena <c>elemental_create</c>.</summary>
    int Create(PlayerEntity master, int classId, int lifetimeMs);
    /// <summary>rAthena <c>elemental_data_received</c>.</summary>
    int DataReceived(PlayerEntity master);
    /// <summary>rAthena <c>elemental_save</c>.</summary>
    int Save(PlayerEntity master);
    /// <summary>rAthena <c>elemental_delete</c>.</summary>
    int Delete(PlayerEntity master);
    /// <summary>rAthena <c>elemental_dead</c>.</summary>
    int Dead(PlayerEntity master);
    /// <summary>rAthena <c>elemental_change_mode</c>.</summary>
    int ChangeMode(PlayerEntity master, int mode);
    /// <summary>rAthena <c>elemental_change_mode_ack</c>.</summary>
    int ChangeModeAck(PlayerEntity master, int mode);
    /// <summary>rAthena <c>elemental_clean_effect</c>.</summary>
    int CleanEffect(PlayerEntity master);
    /// <summary>rAthena <c>elemental_action</c>.</summary>
    int Action(PlayerEntity master, EntityId targetId, long tick);
    /// <summary>rAthena <c>elemental_set_target</c>.</summary>
    int SetTarget(PlayerEntity master, EntityId targetId);
    /// <summary>rAthena <c>elemental_unlocktarget</c>.</summary>
    int UnlockTarget(PlayerEntity master);
    /// <summary>rAthena <c>elemental_heal</c>.</summary>
    void Heal(PlayerEntity master, int hp, int sp);
    /// <summary>rAthena <c>elemental_skillnotok</c>.</summary>
    bool SkillNotOk(PlayerEntity master, ushort skillId);
    /// <summary>rAthena <c>elemental_get_lifetime</c>.</summary>
    long GetLifetimeMs(PlayerEntity master);
    /// <summary>rAthena <c>elemental_summon_init</c>.</summary>
    void SummonInit(PlayerEntity master);
    /// <summary>rAthena <c>elemental_summon_stop</c>.</summary>
    void SummonStop(PlayerEntity master);
}
