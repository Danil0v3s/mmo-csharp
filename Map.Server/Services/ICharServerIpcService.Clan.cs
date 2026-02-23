using Core.Server.IPC;

namespace Map.Server.Services;

public interface ICharServerIpcServiceClan
{
    Task<ClanRequestResponse?> ClanRequestAsync(
        CancellationToken cancellationToken = default);

    Task<ClanMessageResponse?> ClanMessageAsync(
        int clanId,
        int accountId,
        string message,
        CancellationToken cancellationToken = default);

    Task<ClanMemberStateResponse?> ClanMemberLeftAsync(
        int clanId,
        CancellationToken cancellationToken = default);

    Task<ClanMemberStateResponse?> ClanMemberJoinedAsync(
        int clanId,
        CancellationToken cancellationToken = default);
}
