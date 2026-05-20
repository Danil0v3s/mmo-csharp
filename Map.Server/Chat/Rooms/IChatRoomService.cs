using Map.Server.Entities;

namespace Map.Server.Chat.Rooms;

/// <summary>
/// In-world chat rooms (the kind a player places with the
/// <c>/chat</c> command or that an NPC creates via script). Canonical
/// entry points for rAthena <c>chat.cpp</c> (507 lines, 13 functions).
/// </summary>
public interface IChatRoomService
{
    /// <summary>rAthena <c>chat_createpcchat</c>.</summary>
    int CreatePcChat(PlayerEntity owner, string title, string password, int limit, bool pub);

    /// <summary>rAthena <c>chat_createnpcchat</c>.</summary>
    int CreateNpcChat(NpcEntity npc, string title, int limit, bool pub, string eventName);

    /// <summary>rAthena <c>chat_joinchat</c>.</summary>
    bool JoinChat(PlayerEntity pc, int chatId, string password);

    /// <summary>rAthena <c>chat_leavechat</c>.</summary>
    bool LeaveChat(PlayerEntity pc, bool kicked = false);

    /// <summary>rAthena <c>chat_changechatowner</c>.</summary>
    bool ChangeChatOwner(PlayerEntity owner, string newOwnerName);

    /// <summary>rAthena <c>chat_changechatstatus</c>.</summary>
    bool ChangeChatStatus(PlayerEntity owner, string title, string password, int limit, bool pub);

    /// <summary>rAthena <c>chat_kickchat</c>.</summary>
    bool KickChat(PlayerEntity owner, string targetName);

    /// <summary>rAthena <c>chat_deletenpcchat</c>.</summary>
    bool DeleteNpcChat(NpcEntity npc);

    /// <summary>rAthena <c>chat_enableevent</c>.</summary>
    bool EnableEvent(int chatId);

    /// <summary>rAthena <c>chat_disableevent</c>.</summary>
    bool DisableEvent(int chatId);

    /// <summary>rAthena <c>chat_triggerevent</c>.</summary>
    bool TriggerEvent(int chatId);

    /// <summary>rAthena <c>chat_npckickchat</c>.</summary>
    bool NpcKickChat(NpcEntity npc, string targetName);

    /// <summary>rAthena <c>chat_npckickall</c>.</summary>
    int NpcKickAll(NpcEntity npc);
}
