using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Chat.Channels;

/// <summary>
/// Default <see cref="IChannelService"/>. In-memory channel
/// registry; per-PC channel membership tracking. Config file
/// loader (`channels.conf`) data-pending.
/// </summary>
public sealed class ChannelService : IChannelService
{
    private readonly Dictionary<string, ChannelRoom> _rooms = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<EntityId, HashSet<string>> _membership = new();
    private readonly ILogger<ChannelService> _logger;

    public ChannelService(ILogger<ChannelService> logger) => _logger = logger;

    public bool Create(string name, byte type, int ownerId, string passwd, byte color)
    {
        if (_rooms.ContainsKey(name)) return false;
        _rooms[name] = new ChannelRoom { Name = name, Type = type, OwnerId = ownerId, Password = passwd, Color = color };
        return true;
    }

    public int PcCreate(PlayerEntity pc, string name, string passwd)
        => Create(name, 1, pc.AccountId, passwd, 0) ? 0 : -1;

    public bool PcDelete(PlayerEntity pc, string name) => _rooms.Remove(name);

    public int PcJoin(PlayerEntity pc, string name, string passwd)
    {
        if (!_rooms.TryGetValue(name, out var r)) return -1;
        if (!string.IsNullOrEmpty(r.Password) && r.Password != passwd) return -2;
        if (r.Banned.Contains(pc.Id)) return -3;
        r.Members.Add(pc.Id);
        if (!_membership.TryGetValue(pc.Id, out var s)) _membership[pc.Id] = s = new();
        s.Add(name);
        return 0;
    }

    public int PcLeave(PlayerEntity pc, string name)
    {
        if (!_rooms.TryGetValue(name, out var r)) return -1;
        r.Members.Remove(pc.Id);
        if (_membership.TryGetValue(pc.Id, out var s)) s.Remove(name);
        return 0;
    }

    public int PcQuit(PlayerEntity pc)
    {
        if (!_membership.TryGetValue(pc.Id, out var s)) return 0;
        foreach (var n in s.ToArray()) PcLeave(pc, n);
        return 0;
    }

    public int PcKick(PlayerEntity pc, string name, string targetName) => 0;

    public int PcBan(PlayerEntity pc, string name, string targetName)
    {
        // Ban-by-name requires a name → entity lookup; skip until exposed.
        return 0;
    }

    public int PcUnbind(PlayerEntity pc, string name) => 0;
    public int PcBind(PlayerEntity pc, string name) => 0;
    public int PcColor(PlayerEntity pc, string name, byte color) => 0;
    public int PcSetOpt(PlayerEntity pc, string name, int option, int value) => 0;

    public bool PcCheckGroup(PlayerEntity pc, string name) => true;

    public bool PcHasChan(PlayerEntity pc, string name)
        => _membership.TryGetValue(pc.Id, out var s) && s.Contains(name);

    public bool HasPc(string name, PlayerEntity pc)
        => _rooms.TryGetValue(name, out var r) && r.Members.Contains(pc.Id);

    public bool HasPcBanned(string name, PlayerEntity pc)
        => _rooms.TryGetValue(name, out var r) && r.Banned.Contains(pc.Id);

    public int Send(string name, PlayerEntity from, string text)
    {
        if (!_rooms.TryGetValue(name, out var r)) return -1;
        // wire-broadcast data-pending on packet emitter.
        return r.Members.Count;
    }

    public int Join(string name, PlayerEntity pc) => PcJoin(pc, name, "");
    public int AJoin(PlayerEntity pc) => 0;
    public int MJoin(PlayerEntity pc) => 0;
    public int GJoin(PlayerEntity pc) => 0;
    public int Delete(string name) => _rooms.Remove(name) ? 0 : -1;
    public int Check(string name, byte type, int ownerId) => _rooms.ContainsKey(name) ? 1 : 0;
    public int Clean(PlayerEntity pc) => PcQuit(pc);
    public int DisplayList(PlayerEntity pc) => _rooms.Count;
    public void Autojoin(PlayerEntity pc) { }
    public int PcAutojoinSub(PlayerEntity pc, string name) => 0;
    public void ReadConfig() { /* channels.conf loader data-pending */ }
    public bool ReadSub(string name) => true;

    private sealed class ChannelRoom
    {
        public string Name = "";
        public byte Type;
        public int OwnerId;
        public string Password = "";
        public byte Color;
        public HashSet<EntityId> Members = new();
        public HashSet<EntityId> Banned = new();
    }
}
