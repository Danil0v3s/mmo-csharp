using Core.Server.Packets.Out.ZC;
using Map.Server.Entities;
using Map.Server.Session;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Chat;

/// <summary>
/// First-slice <see cref="IChatService"/>. Whisper/party/guild routing
/// uses local same-map delivery first, then hands off to char IPC for
/// any recipients we can't see locally. Mirrors rAthena
/// <c>party_send_message</c> / <c>guild_send_message</c> /
/// <c>clif_wis_message</c>.
/// </summary>
public sealed class ChatService : IChatService
{
    private readonly IEntityRegistry _entities;
    private readonly ISessionManagerAccessor _sessions;
    private readonly IChatIpcOutbound _ipc;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IEntityRegistry entities,
        ISessionManagerAccessor sessions,
        IChatIpcOutbound ipc,
        ILogger<ChatService> logger)
    {
        _entities = entities;
        _sessions = sessions;
        _ipc = ipc;
        _logger = logger;
    }

    public async Task<ChatDeliveryResult> SendWhisperAsync(
        PlayerEntity sender, MapSessionData senderSession,
        string targetName, string message,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(targetName)) return ChatDeliveryResult.TargetOffline;

        // Local delivery first — scan the on-map players for a name match
        // (rAthena map_nick2sd path).
        var local = FindLocalPlayerByName(targetName);
        if (local is { } target)
        {
            var targetSession = _sessions.GetByEntityId(target.Id);
            if (targetSession != null)
            {
                targetSession.EnqueuePacket(new ZC_WHISPER
                {
                    SenderAccountId = sender.AccountId,
                    SenderName = sender.Name,
                    IsAdmin = (byte)(senderSession.GroupId >= 99 ? 1 : 0),
                    Message = message,
                });
                return ChatDeliveryResult.DeliveredLocal;
            }
        }

        // Off-map / unknown — hand off to char so it can route across
        // map servers. The InterWhisper IPC returns nothing about whether
        // it eventually landed; rAthena emits WIS_TARGET_OFFLINE only when
        // even the char server can't see the recipient.
        var ok = await _ipc.WhisperAsync(
            sender.AccountId, sender.CharacterId,
            sender.Name, targetName, message, ct);
        return ok ? ChatDeliveryResult.DeliveredCrossServer : ChatDeliveryResult.TargetOffline;
    }

    public async Task SendPartyAsync(PlayerEntity sender, string message, CancellationToken ct = default)
    {
        if (sender.PartyId == 0) return;

        // Local fan-out: any party member on this map server gets the
        // packet right away. The IPC fan-out below will not reach back
        // here.
        BroadcastToLocalMembers(
            isParty: true,
            affiliationId: sender.PartyId,
            packet: () => new ZC_NOTIFY_CHAT_PARTY
            {
                AccountId = sender.AccountId,
                Message = message,
            });

        // Cross-server: hand off to char which routes to peer map
        // servers' MapPartyMessage IPC. Failure here is logged but
        // doesn't block local delivery (matches rAthena where the inter
        // hop is best-effort).
        var ok = await _ipc.PartyMessageAsync(sender.PartyId, sender.AccountId, message, ct);
        if (!ok)
        {
            _logger.LogDebug(
                "PartyMessage IPC missed: party {Party} from {Char}",
                sender.PartyId, sender.CharacterId);
        }
    }

    public async Task SendGuildAsync(PlayerEntity sender, string message, CancellationToken ct = default)
    {
        if (sender.GuildId == 0) return;

        BroadcastToLocalMembers(
            isParty: false,
            affiliationId: sender.GuildId,
            packet: () => new ZC_GUILD_CHAT { Message = message });

        var ok = await _ipc.GuildMessageAsync(sender.GuildId, sender.AccountId, message, ct);
        if (!ok)
        {
            _logger.LogDebug(
                "GuildMessage IPC missed: guild {Guild} from {Char}",
                sender.GuildId, sender.CharacterId);
        }
    }

    // --- helpers ---

    private PlayerEntity? FindLocalPlayerByName(string name)
    {
        foreach (var e in _entities.All())
        {
            if (e is PlayerEntity pc && string.Equals(pc.Name, name, StringComparison.Ordinal))
                return pc;
        }
        return null;
    }

    /// <summary>
    /// Send <paramref name="packet"/> to every locally-loaded PC who
    /// shares the affiliation id with the sender. Builds a fresh packet
    /// per recipient (the outbound queue takes ownership of the bytes).
    /// </summary>
    private void BroadcastToLocalMembers(bool isParty, int affiliationId, Func<Core.Server.Packets.OutgoingPacket> packet)
    {
        foreach (var e in _entities.All())
        {
            if (e is not PlayerEntity pc) continue;
            var match = isParty ? pc.PartyId == affiliationId : pc.GuildId == affiliationId;
            if (!match) continue;
            var session = _sessions.GetByEntityId(pc.Id);
            session?.EnqueuePacket(packet());
        }
    }
}
