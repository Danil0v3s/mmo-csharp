using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Clan;

/// <summary>
/// Default <see cref="IClanService"/>. Tracks per-player ClanId on
/// PlayerEntity; chat broadcast walks the entity registry filtering
/// by ClanId. The actual roster lives on the Char server side
/// (clan_db.yml + ClanMembersAsync IPC); cross-server chat goes
/// through the inter routing service when the consumer ports.
/// </summary>
public sealed class ClanService : IClanService
{
    private readonly IEntityRegistry _entities;
    private readonly ILogger<ClanService> _logger;

    public ClanService(IEntityRegistry entities, ILogger<ClanService> logger)
    {
        _entities = entities;
        _logger = logger;
    }

    public bool MemberJoin(PlayerEntity pc, int clanId)
    {
        pc.ClanId = clanId;
        return true;
    }

    public bool MemberLeave(PlayerEntity pc)
    {
        pc.ClanId = 0;
        return true;
    }

    public void MemberJoined(PlayerEntity pc) { /* clan-update broadcast pending */ }
    public void MemberLeft(PlayerEntity pc) { /* clan-update broadcast pending */ }
    public void LoadClanData(PlayerEntity pc) { /* IPC load from char server pending */ }

    public int GetMemberIndex(int clanId, PlayerEntity pc) => -1;
    public int GetNextFreeMemberIndex(int clanId) => -1;
    public int GetAllianceCount(int clanId) => 0;

    public PlayerEntity? GetAvailableMember(int clanId)
    {
        foreach (var e in _entities.All())
            if (e is PlayerEntity p && p.ClanId == clanId) return p;
        return null;
    }

    public void SendMessage(PlayerEntity from, string text)
    {
        if (from.ClanId == 0) return;
        foreach (var e in _entities.All())
        {
            if (e is PlayerEntity p && p.ClanId == from.ClanId)
            {
                // wire packet: ZC_NOTIFY_CHAT_PARTY-equivalent for clan.
                // Data-pending on a ClanChatPacket emitter.
            }
        }
    }

    public void RecvMessage(int clanId, string sender, string text)
    {
        // Cross-server fan-out. The inter routing service receives the
        // packet and forwards it here; we re-broadcast to local members.
    }
}
