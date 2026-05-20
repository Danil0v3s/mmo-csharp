using Map.Server.Entities;

namespace Map.Server.Mercenary;

/// <summary>
/// Mercenary companion lifecycle. Canonical entry points for rAthena
/// <c>mercenary.cpp</c> (956 lines, 19 functions).
/// </summary>
public interface IMercenaryService
{
    /// <summary>rAthena <c>mercenary_create</c> — spawn a merc.</summary>
    bool Create(PlayerEntity master, int classId, int lifetimeMs);

    /// <summary>rAthena <c>mercenary_dead</c>.</summary>
    bool Dead(PlayerEntity master);

    /// <summary>rAthena <c>mercenary_delete</c>.</summary>
    int Delete(PlayerEntity master, byte reason);

    /// <summary>rAthena <c>mercenary_recv_data</c> — hydrate from char-server.</summary>
    bool RecvData(PlayerEntity master);

    /// <summary>rAthena <c>mercenary_save</c>.</summary>
    void Save(PlayerEntity master);

    /// <summary>rAthena <c>mercenary_get_calls</c>.</summary>
    int GetCalls(int classId);

    /// <summary>rAthena <c>mercenary_set_calls</c>.</summary>
    void SetCalls(PlayerEntity master, int delta);

    /// <summary>rAthena <c>mercenary_get_faith</c>.</summary>
    int GetFaith(PlayerEntity master);

    /// <summary>rAthena <c>mercenary_set_faith</c>.</summary>
    void SetFaith(PlayerEntity master, int delta);

    /// <summary>rAthena <c>mercenary_get_lifetime</c>.</summary>
    long GetLifetimeMs(PlayerEntity master);

    /// <summary>rAthena <c>mercenary_heal</c>.</summary>
    void Heal(PlayerEntity master, int hp, int sp);

    /// <summary>rAthena <c>mercenary_killbonus</c>.</summary>
    void KillBonus(PlayerEntity master);

    /// <summary>rAthena <c>mercenary_kills</c>.</summary>
    void Kills(PlayerEntity master);

    /// <summary>rAthena <c>mercenary_checkskill</c>.</summary>
    ushort CheckSkill(PlayerEntity master, ushort skillId);

    /// <summary>rAthena <c>merc_contract_init</c>.</summary>
    void ContractInit(PlayerEntity master);

    /// <summary>rAthena <c>mercenary_contract_stop</c>.</summary>
    void ContractStop(PlayerEntity master);
}
