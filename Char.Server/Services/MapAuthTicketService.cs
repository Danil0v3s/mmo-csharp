namespace Char.Server.Services;

public class MapAuthTicketService : IMapAuthTicketService
{
    private readonly Dictionary<int, MapAuthTicket> _tickets = new();
    private readonly object _lock = new();

    public bool IssueTicket(
        int accountId,
        long characterId,
        int loginId1,
        int loginId2,
        uint sex,
        uint clientType,
        int ttlSeconds)
    {
        if (accountId <= 0 || characterId <= 0)
        {
            return false;
        }

        var expiresAt = DateTime.UtcNow.AddSeconds(ttlSeconds <= 0 ? 60 : ttlSeconds);
        var ticket = new MapAuthTicket(
            AccountId: accountId,
            CharacterId: characterId,
            LoginId1: loginId1,
            LoginId2: loginId2,
            Sex: sex,
            ClientType: clientType,
            ExpiresAtUtc: expiresAt);

        lock (_lock)
        {
            _tickets[accountId] = ticket;
        }

        return true;
    }

    public bool TryConsumeTicket(
        int accountId,
        long characterId,
        int loginId1,
        int loginId2,
        out uint sex,
        out uint clientType)
    {
        lock (_lock)
        {
            if (!_tickets.TryGetValue(accountId, out var ticket))
            {
                sex = 0;
                clientType = 0;
                return false;
            }

            if (ticket.ExpiresAtUtc < DateTime.UtcNow ||
                ticket.CharacterId != characterId ||
                ticket.LoginId1 != loginId1 ||
                ticket.LoginId2 != loginId2)
            {
                _tickets.Remove(accountId);
                sex = 0;
                clientType = 0;
                return false;
            }

            _tickets.Remove(accountId);
            sex = ticket.Sex;
            clientType = ticket.ClientType;
            return true;
        }
    }

    private record MapAuthTicket(
        int AccountId,
        long CharacterId,
        int LoginId1,
        int LoginId2,
        uint Sex,
        uint ClientType,
        DateTime ExpiresAtUtc
    );
}
