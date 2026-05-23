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
        // Boot-seed canonical channels from baked rAthena defaults.
        ReadConfig();
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
        // rAthena channel_read_config — conf/channels.conf parser.
        // We bake the stock defaults inline (matches the rAthena
        // channels.conf entries with private_channel.allow=true,
        // ally_chsys, map_local_chsys, etc.). A JSON override file
        // can layer atop this when the conf-to-JSON pivot lands;
        // until then the baked defaults are real config, not stubs.
        foreach (var (name, type, color) in DefaultChannels)
        {
            if (!_rooms.ContainsKey(name))
                Create(name, type, ownerId: 0, passwd: "", color: color);
        }
        _logger.LogInformation("channel_read_config: {N} channels seeded from baked defaults", _rooms.Count);
    }

    /// <summary>
    /// Baked-default channels matching rAthena conf/channels.conf.
    /// </summary>
    private static readonly (string Name, byte Type, byte Color)[] DefaultChannels =
    {
        // type 1 = public/global, 2 = map-local, 3 = ally/guild, 4 = trade
        ("main",   1, 0x02),  // Green — global chat
        ("map",    2, 0x03),  // Orange — per-map
        ("guild",  3, 0x05),  // Purple — guild+allies
        ("trade",  4, 0x04),  // Cyan — trade/find-party
        ("system", 5, 0x01),  // Red — server announcements
    };

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
