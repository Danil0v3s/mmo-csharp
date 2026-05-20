namespace Map.Server.Logging;

/// <summary>
/// Game-event auditing (atcommand / chat / pick / zeny / mvp drops /
/// cash / branch / feeding / NPC). Canonical entry points for
/// rAthena <c>log.cpp</c> (718 lines, 13 functions).
///
/// rAthena writes these to dedicated SQL tables (`atcommandlog`,
/// `picklog`, `zenylog`, `mvplog`, `chatlog`, `cashlog`,
/// `branchlog`, `feedinglog`, `npclog`). The C# port already has an
/// <see cref="Gm.AtCommandLogger"/> for the atcommand table; the
/// other tables data-pending on their EF Core entities.
/// </summary>
public interface IGameLogService
{
    /// <summary>rAthena <c>log_atcommand</c>.</summary>
    void Atcommand(int accountId, int charId, string charName, string command);

    /// <summary>rAthena <c>log_branch</c> — monster summon log.</summary>
    void Branch(int accountId, int charId, string mapName, int mobId);

    /// <summary>rAthena <c>log_cash</c>.</summary>
    void Cash(int accountId, int charId, long amount, string reason);

    /// <summary>rAthena <c>log_chat</c>.</summary>
    void Chat(string scope, int srcId, int dstId, string text);

    /// <summary>rAthena <c>log_feeding</c> — pet / homun feed log.</summary>
    void Feeding(int accountId, int charId, byte target, int itemId);

    /// <summary>rAthena <c>log_mvpdrop</c>.</summary>
    void MvpDrop(int charId, int mobId, int itemId);

    /// <summary>rAthena <c>log_npc</c> — script-triggered log.</summary>
    void Npc(int accountId, int charId, string text);

    /// <summary>rAthena <c>log_pick</c> — generic pick log.</summary>
    void Pick(byte who, int srcId, char operation, int itemId, int amount);

    /// <summary>rAthena <c>log_pick_pc</c>.</summary>
    void PickPc(int charId, char operation, int itemId, int amount);

    /// <summary>rAthena <c>log_pick_mob</c>.</summary>
    void PickMob(int mobId, char operation, int itemId, int amount);

    /// <summary>rAthena <c>log_zeny</c>.</summary>
    void Zeny(int srcCharId, int dstCharId, long amount, string reason);

    /// <summary>rAthena <c>log_set_defaults</c>.</summary>
    void SetDefaults();

    /// <summary>rAthena <c>log_config_read</c>.</summary>
    bool ConfigRead(string configPath);
}
