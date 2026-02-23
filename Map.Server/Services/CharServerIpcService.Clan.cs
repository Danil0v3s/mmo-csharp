using Core.Server.IPC;

namespace Map.Server.Services;

public partial class CharServerIpcService
{
    public async Task<ClanRequestResponse?> ClanRequestAsync(
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.ClanRequestAsync(new ClanRequestRequest(), cancellationToken: cancellationToken);
    }

    public async Task<ClanMessageResponse?> ClanMessageAsync(
        int clanId,
        int accountId,
        string message,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.ClanMessageAsync(new ClanMessageRequest
        {
            ClanId = clanId,
            AccountId = accountId,
            Message = message ?? string.Empty
        }, cancellationToken: cancellationToken);
    }

    public async Task<ClanMemberStateResponse?> ClanMemberLeftAsync(
        int clanId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.ClanMemberLeftAsync(new ClanMemberStateRequest
        {
            ClanId = clanId
        }, cancellationToken: cancellationToken);
    }

    public async Task<ClanMemberStateResponse?> ClanMemberJoinedAsync(
        int clanId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.ClanMemberJoinedAsync(new ClanMemberStateRequest
        {
            ClanId = clanId
        }, cancellationToken: cancellationToken);
    }
}
