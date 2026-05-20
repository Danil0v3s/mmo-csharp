using Map.Server.Entities;

namespace Map.Server.Scripting.NpcChat;

/// <summary>
/// NPC chat / pattern-matching subsystem (PCRE patterns on player
/// dialogue triggering NPC events). Canonical entry points for
/// rAthena <c>npc_chat.cpp</c> (443 lines, 8 functions).
///
/// rAthena links to <c>libpcre</c> for regex; the C# port uses
/// <see cref="System.Text.RegularExpressions.Regex"/>. The script
/// engine has its own NPC event dispatch; these methods wire pattern
/// adds / removes from `defpattern` / `activatepset` BUILTINs.
/// </summary>
public interface INpcChatService
{
    /// <summary>rAthena <c>buildin_defpattern</c> — define a chat pattern -> event mapping.</summary>
    bool DefPattern(NpcEntity npc, int setId, string pattern, string eventName);

    /// <summary>rAthena <c>buildin_activatepset</c>.</summary>
    bool ActivatePset(NpcEntity npc, int setId);

    /// <summary>rAthena <c>buildin_deactivatepset</c>.</summary>
    bool DeactivatePset(NpcEntity npc, int setId);

    /// <summary>rAthena <c>buildin_deletepset</c>.</summary>
    bool DeletePset(NpcEntity npc, int setId);

    /// <summary>rAthena <c>npc_chat_sub</c> — incoming chat checked against patterns.</summary>
    int CheckChat(NpcEntity npc, PlayerEntity speaker, string text);

    /// <summary>rAthena <c>npc_chat_def_pattern</c> — boot-time default-pattern install.</summary>
    void DefaultPattern(NpcEntity npc);

    /// <summary>rAthena <c>npc_chat_finalize</c>.</summary>
    void Finalize(NpcEntity npc);

    /// <summary>rAthena <c>finalize_pcrematch_entry</c>.</summary>
    void FinalizeEntry(NpcEntity npc, int setId, string pattern);
}
