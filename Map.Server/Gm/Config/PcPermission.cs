namespace Map.Server.Gm.Config;

/// <summary>
/// Mirror of rAthena <c>e_pc_permission</c> (pc_groups.hpp:23). 31
/// permission flags loaded per-group from <c>conf/groups.yml</c>. Names
/// in the YAML map to <see cref="ToYamlKey"/>.
/// </summary>
public enum PcPermission
{
    CanTrade,                  // can_trade
    CanParty,                  // can_party
    AllSkill,                  // all_skill
    AllEquipment,              // all_equipment
    SkillUnconditional,        // skill_unconditional
    JoinAllChat,               // join_chat
    NoChatKick,                // kick_chat
    HideSession,               // hide_session
    WhoDisplayAid,             // who_display_aid
    ReceiveHackInfo,           // hack_info
    WarpAnywhere,              // any_warp
    ViewHpMeter,               // view_hpmeter
    ViewEquipment,             // view_equipment
    UseCheck,                  // use_check
    UseChangemapType,          // use_changemaptype
    UseAllCommands,            // all_commands
    ReceiveRequests,           // receive_requests
    ShowBossMobs,              // show_bossmobs
    DisablePvm,                // disable_pvm
    DisablePvp,                // disable_pvp
    DisableCmdDead,            // disable_commands_when_dead
    ChannelAdmin,              // channel_admin
    CanTradeBounded,           // can_trade_bounded
    ItemUnconditional,         // item_unconditional
    EnableCommand,             // command_enable
    BypassStatOnClone,         // bypass_stat_onclone
    BypassMaxStat,             // bypass_max_stat
    Attendance,                // attendance
    MacroDetect,               // macro_detect
    MacroRegister,             // macro_register
    TradeUnconditional,        // trade_unconditional
}

public static class PcPermissionExtensions
{
    /// <summary>
    /// rAthena's <c>pc_g_permission_name[]</c> table maps each enum value
    /// to its YAML key. We mirror that table here so the loader can parse
    /// the string keys without a separate lookup.
    /// </summary>
    public static string ToYamlKey(this PcPermission p) => p switch
    {
        PcPermission.CanTrade => "can_trade",
        PcPermission.CanParty => "can_party",
        PcPermission.AllSkill => "all_skill",
        PcPermission.AllEquipment => "all_equipment",
        PcPermission.SkillUnconditional => "skill_unconditional",
        PcPermission.JoinAllChat => "join_chat",
        PcPermission.NoChatKick => "kick_chat",
        PcPermission.HideSession => "hide_session",
        PcPermission.WhoDisplayAid => "who_display_aid",
        PcPermission.ReceiveHackInfo => "hack_info",
        PcPermission.WarpAnywhere => "any_warp",
        PcPermission.ViewHpMeter => "view_hpmeter",
        PcPermission.ViewEquipment => "view_equipment",
        PcPermission.UseCheck => "use_check",
        PcPermission.UseChangemapType => "use_changemaptype",
        PcPermission.UseAllCommands => "all_commands",
        PcPermission.ReceiveRequests => "receive_requests",
        PcPermission.ShowBossMobs => "show_bossmobs",
        PcPermission.DisablePvm => "disable_pvm",
        PcPermission.DisablePvp => "disable_pvp",
        PcPermission.DisableCmdDead => "disable_commands_when_dead",
        PcPermission.ChannelAdmin => "channel_admin",
        PcPermission.CanTradeBounded => "can_trade_bounded",
        PcPermission.ItemUnconditional => "item_unconditional",
        PcPermission.EnableCommand => "command_enable",
        PcPermission.BypassStatOnClone => "bypass_stat_onclone",
        PcPermission.BypassMaxStat => "bypass_max_stat",
        PcPermission.Attendance => "attendance",
        PcPermission.MacroDetect => "macro_detect",
        PcPermission.MacroRegister => "macro_register",
        PcPermission.TradeUnconditional => "trade_unconditional",
        _ => p.ToString().ToLowerInvariant(),
    };

    public static bool TryParse(string yamlKey, out PcPermission perm)
    {
        foreach (PcPermission p in Enum.GetValues<PcPermission>())
        {
            if (string.Equals(p.ToYamlKey(), yamlKey, StringComparison.OrdinalIgnoreCase))
            {
                perm = p;
                return true;
            }
        }
        perm = default;
        return false;
    }
}
