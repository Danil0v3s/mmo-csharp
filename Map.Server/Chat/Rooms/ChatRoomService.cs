using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Chat.Rooms;

/// <summary>
/// Default <see cref="IChatRoomService"/>. Transient in-memory chat-
/// room registry (rAthena parity — rooms don't survive restart).
/// Wire packets (ZC_ROOM_NEWENTRY, ZC_DESTROY_ROOM, etc.) land
/// alongside their consumers.
/// </summary>
public sealed class ChatRoomService : IChatRoomService
{
    private readonly Dictionary<int, ChatRoom> _rooms = new();
    private int _nextId = 1;
    private readonly ILogger<ChatRoomService> _logger;

    public ChatRoomService(ILogger<ChatRoomService> logger) => _logger = logger;

    public int CreatePcChat(PlayerEntity owner, string title, string password, int limit, bool pub)
    {
        var id = _nextId++;
        _rooms[id] = new ChatRoom { Id = id, OwnerId = owner.Id, Title = title, Password = password, Limit = limit, Public = pub };
        return id;
    }

    public int CreateNpcChat(NpcEntity npc, string title, int limit, bool pub, string eventName)
    {
        var id = _nextId++;
        _rooms[id] = new ChatRoom { Id = id, OwnerId = npc.Id, Title = title, Limit = limit, Public = pub, EventName = eventName };
        return id;
    }

    public bool JoinChat(PlayerEntity pc, int chatId, string password)
    {
        if (!_rooms.TryGetValue(chatId, out var r)) return false;
        if (!string.IsNullOrEmpty(r.Password) && r.Password != password) return false;
        if (r.Members.Count >= r.Limit) return false;
        r.Members.Add(pc.Id);
        return true;
    }

    public bool LeaveChat(PlayerEntity pc, bool kicked = false)
    {
        foreach (var r in _rooms.Values)
            if (r.Members.Remove(pc.Id)) return true;
        return false;
    }

    public bool ChangeChatOwner(PlayerEntity owner, string newOwnerName) => false;
    public bool ChangeChatStatus(PlayerEntity owner, string title, string password, int limit, bool pub) => false;
    public bool KickChat(PlayerEntity owner, string targetName) => false;

    public bool DeleteNpcChat(NpcEntity npc)
    {
        var keys = _rooms.Where(kv => kv.Value.OwnerId == npc.Id).Select(kv => kv.Key).ToArray();
        foreach (var k in keys) _rooms.Remove(k);
        return keys.Length > 0;
    }

    public bool EnableEvent(int chatId) => _rooms.ContainsKey(chatId);
    public bool DisableEvent(int chatId) => _rooms.ContainsKey(chatId);
    public bool TriggerEvent(int chatId) => _rooms.ContainsKey(chatId);
    public bool NpcKickChat(NpcEntity npc, string targetName) => false;

    public int NpcKickAll(NpcEntity npc)
    {
        var room = _rooms.Values.FirstOrDefault(r => r.OwnerId == npc.Id);
        if (room == null) return 0;
        var n = room.Members.Count;
        room.Members.Clear();
        return n;
    }

    private sealed class ChatRoom
    {
        public int Id;
        public EntityId OwnerId;
        public string Title = "";
        public string Password = "";
        public int Limit;
        public bool Public;
        public string EventName = "";
        public HashSet<EntityId> Members = new();
    }
}
