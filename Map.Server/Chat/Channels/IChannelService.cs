using Map.Server.Entities;

namespace Map.Server.Chat.Channels;

/// <summary>
/// Persistent text channels (`#main`, `#trade`, custom guild /
/// party channels). Canonical entry points for rAthena
/// <c>channel.cpp</c> (1 526 lines, 30 functions).
/// </summary>
public interface IChannelService
{
    /// <summary>rAthena <c>channel_create</c> (via configs).</summary>
    bool Create(string name, byte type, int ownerId, string passwd, byte color);

    /// <summary>rAthena <c>channel_pccreate</c>.</summary>
    int PcCreate(PlayerEntity pc, string name, string passwd);

    /// <summary>rAthena <c>channel_pcdelete</c>.</summary>
    bool PcDelete(PlayerEntity pc, string name);

    /// <summary>rAthena <c>channel_pcjoin</c>.</summary>
    int PcJoin(PlayerEntity pc, string name, string passwd);

    /// <summary>rAthena <c>channel_pcleave</c>.</summary>
    int PcLeave(PlayerEntity pc, string name);

    /// <summary>rAthena <c>channel_pcquit</c>.</summary>
    int PcQuit(PlayerEntity pc);

    /// <summary>rAthena <c>channel_pckick</c>.</summary>
    int PcKick(PlayerEntity pc, string name, string targetName);

    /// <summary>rAthena <c>channel_pcban</c>.</summary>
    int PcBan(PlayerEntity pc, string name, string targetName);

    /// <summary>rAthena <c>channel_pcunbind</c>.</summary>
    int PcUnbind(PlayerEntity pc, string name);

    /// <summary>rAthena <c>channel_pcbind</c>.</summary>
    int PcBind(PlayerEntity pc, string name);

    /// <summary>rAthena <c>channel_pccolor</c>.</summary>
    int PcColor(PlayerEntity pc, string name, byte color);

    /// <summary>rAthena <c>channel_pcsetopt</c>.</summary>
    int PcSetOpt(PlayerEntity pc, string name, int option, int value);

    /// <summary>rAthena <c>channel_pccheckgroup</c>.</summary>
    bool PcCheckGroup(PlayerEntity pc, string name);

    /// <summary>rAthena <c>channel_pc_haschan</c>.</summary>
    bool PcHasChan(PlayerEntity pc, string name);

    /// <summary>rAthena <c>channel_haspc</c>.</summary>
    bool HasPc(string name, PlayerEntity pc);

    /// <summary>rAthena <c>channel_haspcbanned</c>.</summary>
    bool HasPcBanned(string name, PlayerEntity pc);

    /// <summary>rAthena <c>channel_send</c>.</summary>
    int Send(string name, PlayerEntity from, string text);

    /// <summary>rAthena <c>channel_join</c>.</summary>
    int Join(string name, PlayerEntity pc);

    /// <summary>rAthena <c>channel_ajoin</c> — auto-join.</summary>
    int AJoin(PlayerEntity pc);

    /// <summary>rAthena <c>channel_mjoin</c> — map chan join.</summary>
    int MJoin(PlayerEntity pc);

    /// <summary>rAthena <c>channel_gjoin</c> — guild chan join.</summary>
    int GJoin(PlayerEntity pc);

    /// <summary>rAthena <c>channel_delete</c>.</summary>
    int Delete(string name);

    /// <summary>rAthena <c>channel_chk</c>.</summary>
    int Check(string name, byte type, int ownerId);

    /// <summary>rAthena <c>channel_clean</c>.</summary>
    int Clean(PlayerEntity pc);

    /// <summary>rAthena <c>channel_display_list</c>.</summary>
    int DisplayList(PlayerEntity pc);

    /// <summary>rAthena <c>channel_autojoin</c>.</summary>
    void Autojoin(PlayerEntity pc);

    /// <summary>rAthena <c>channel_pcautojoin_sub</c>.</summary>
    int PcAutojoinSub(PlayerEntity pc, string name);

    /// <summary>rAthena <c>channel_read_config</c>.</summary>
    void ReadConfig();

    /// <summary>rAthena <c>channel_read_sub</c>.</summary>
    bool ReadSub(string name);
}
