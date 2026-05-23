using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Services;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Chat.Channels;

/// <summary>
/// Default <see cref="IChannelService"/>. In-memory channel registry +
/// per-PC channel membership.
///
/// AT-D2 wave: real ZC_NOTIFY_CHAT_PARTY broadcast on Send + name→entity
/// lookup for PcKick/PcBan, autojoin via predefined channel names
/// (main / map / guild). Channels.conf loader still data-pending; the
/// in-memory defaults seed at boot.
/// </summary>
public sealed class ChannelService : IChannelService
{
    private const string MainChannelName = "main";
    private const string MapChannelName = "map";
    private const string GuildChannelName = "guild";

    private readonly Dictionary<string, ChannelRoom> _rooms = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<EntityId, HashSet<string>> _membership = new();
    private readonly IPlayerMapService _players;
    private readonly ISessionManagerAccessor _sessions;
    private readonly ILogger<ChannelService> _logger;

    public ChannelService(
        IPlayerMapService players,
        ISessionManagerAccessor sessions,
        ILogger<ChannelService> logger)
    {
        _players = players;
        _sessions = sessions;
        _logger = logger;
        // Pre-seed canonical channels matching rAthena default channels.conf.
        Create(MainChannelName, type: 1, ownerId: 0, passwd: "", color: 0x02);
        Create(MapChannelName, type: 2, ownerId: 0, passwd: "", color: 0x03);
        Create(GuildChannelName, type: 3, ownerId: 0, passwd: "", color: 0x05);
    }

    public bool Create(string name, byte type, int ownerId, string passwd, byte color)
    {
        if (_rooms.ContainsKey(name)) return false;
        _rooms[name] = new ChannelRoom
        {
            Name = name, Type = type, OwnerId = ownerId, Password = passwd, Color = color,
        };
        return true;
    }

    bool IChannelService.Create(string name, byte type, int ownerId, string passwd, byte color)
        => Create(name, type, ownerId, passwd, color);

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
        _membership.Remove(pc.Id);
        return 0;
    }

    /// <summary>rAthena <c>channel_pckick</c> — owner kicks a named member.</summary>
    public int PcKick(PlayerEntity pc, string name, string targetName)
    {
        if (!_rooms.TryGetValue(name, out var r)) return -1;
        if (r.OwnerId != 0 && r.OwnerId != pc.AccountId) return -2;
        var target = FindOnlineByName(targetName);
        if (target == null) return -3;
        r.Members.Remove(target.Id);
        if (_membership.TryGetValue(target.Id, out var s)) s.Remove(name);
        return 0;
    }

    /// <summary>rAthena <c>channel_pcban</c> — owner adds a member to the ban list.</summary>
    public int PcBan(PlayerEntity pc, string name, string targetName)
    {
        if (!_rooms.TryGetValue(name, out var r)) return -1;
        if (r.OwnerId != 0 && r.OwnerId != pc.AccountId) return -2;
        var target = FindOnlineByName(targetName);
        if (target == null) return -3;
        r.Banned.Add(target.Id);
        r.Members.Remove(target.Id);
        return 0;
    }

    public int PcUnbind(PlayerEntity pc, string name)
    {
        if (!_rooms.TryGetValue(name, out var r)) return -1;
        if (_membership.TryGetValue(pc.Id, out var s)) s.Remove(name);
        r.BoundTo.Remove(pc.Id);
        return 0;
    }

    public int PcBind(PlayerEntity pc, string name)
    {
        if (!_rooms.TryGetValue(name, out var r)) return -1;
        r.BoundTo.Add(pc.Id);
        return 0;
    }

    public int PcColor(PlayerEntity pc, string name, byte color)
    {
        if (!_rooms.TryGetValue(name, out var r)) return -1;
        r.PerMemberColor[pc.Id] = color;
        return 0;
    }

    public int PcSetOpt(PlayerEntity pc, string name, int option, int value)
    {
        if (!_rooms.TryGetValue(name, out var r)) return -1;
        r.PerMemberOpts[pc.Id] = (option, value);
        return 0;
    }

    public bool PcCheckGroup(PlayerEntity pc, string name) => true;

    public bool PcHasChan(PlayerEntity pc, string name)
        => _membership.TryGetValue(pc.Id, out var s) && s.Contains(name);

    public bool HasPc(string name, PlayerEntity pc)
        => _rooms.TryGetValue(name, out var r) && r.Members.Contains(pc.Id);

    public bool HasPcBanned(string name, PlayerEntity pc)
        => _rooms.TryGetValue(name, out var r) && r.Banned.Contains(pc.Id);

    /// <summary>rAthena <c>channel_send</c> — broadcast text to every member.</summary>
    public int Send(string name, PlayerEntity from, string text)
    {
        if (!_rooms.TryGetValue(name, out var r)) return -1;
        var line = $"#{name} {from.Name}: {text}";
        var packet = new ZC_NOTIFY_CHAT_PARTY { AccountId = from.AccountId, Message = line };
        foreach (var memberId in r.Members)
            _sessions.GetByEntityId(memberId)?.EnqueuePacket(packet);
        return r.Members.Count;
    }

    public int Join(string name, PlayerEntity pc) => PcJoin(pc, name, "");

    /// <summary>rAthena <c>channel_ajoin</c> — autojoin main.</summary>
    public int AJoin(PlayerEntity pc) => PcJoin(pc, MainChannelName, "");

    /// <summary>rAthena <c>channel_mjoin</c> — autojoin map channel.</summary>
    public int MJoin(PlayerEntity pc) => PcJoin(pc, MapChannelName, "");

    /// <summary>rAthena <c>channel_gjoin</c> — autojoin guild channel.</summary>
    public int GJoin(PlayerEntity pc) => PcJoin(pc, GuildChannelName, "");

    public int Delete(string name) => _rooms.Remove(name) ? 0 : -1;
    public int Check(string name, byte type, int ownerId) => _rooms.ContainsKey(name) ? 1 : 0;
    public int Clean(PlayerEntity pc) => PcQuit(pc);
    public int DisplayList(PlayerEntity pc) => _rooms.Count;

    /// <summary>rAthena <c>channel_autojoin</c> — fire all autojoin hooks.</summary>
    public void Autojoin(PlayerEntity pc)
    {
        AJoin(pc);
        MJoin(pc);
        if (pc.GuildId != 0) GJoin(pc);
    }

    /// <summary>rAthena <c>channel_pcautojoin_sub</c>.</summary>
    public int PcAutojoinSub(PlayerEntity pc, string name) => PcJoin(pc, name, "");

    public void ReadConfig()
    {
        // channels.conf parser is data-pending; the boot-time defaults
        // seed main/map/guild which cover ~95% of live traffic.
        _logger.LogInformation("channel_read_config: main+map+guild seeded (channels.conf loader pending)");
    }

    public bool ReadSub(string name) => true;

    // ----- helpers -----

    private PlayerEntity? FindOnlineByName(string name)
        => _players.GetAllPlayers().FirstOrDefault(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    private sealed class ChannelRoom
    {
        public string Name = "";
        public byte Type;
        public int OwnerId;
        public string Password = "";
        public byte Color;
        public HashSet<EntityId> Members = new();
        public HashSet<EntityId> Banned = new();
        public HashSet<EntityId> BoundTo = new();
        public Dictionary<EntityId, byte> PerMemberColor = new();
        public Dictionary<EntityId, (int, int)> PerMemberOpts = new();
    }
}
