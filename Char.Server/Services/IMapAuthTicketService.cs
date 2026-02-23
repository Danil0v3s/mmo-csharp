namespace Char.Server.Services;

/// <summary>
/// Manages auth tickets for map server authentication.
/// </summary>
public interface IMapAuthTicketService
{
    bool IssueTicket(
        int accountId,
        long characterId,
        int loginId1,
        int loginId2,
        uint sex,
        uint clientType,
        int ttlSeconds);

    bool TryConsumeTicket(
        int accountId,
        long characterId,
        int loginId1,
        int loginId2,
        out uint sex,
        out uint clientType);
}
