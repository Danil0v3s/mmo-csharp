using System.Collections.Concurrent;

namespace Char.Server.Services;

public sealed class ReturningClientAuthService : IReturningClientAuthService
{
    private readonly ConcurrentDictionary<int, Entry> _entries = new();
    private readonly ILogger<ReturningClientAuthService> _logger;

    public ReturningClientAuthService(ILogger<ReturningClientAuthService> logger)
    {
        _logger = logger;
    }

    public void Allow(int accountId, int loginId1, byte sex)
    {
        _entries[accountId] = new Entry(loginId1, sex, DateTime.UtcNow);
        _logger.LogInformation(
            "ReturningClientAuth.Allow: account {AccountId}, loginId1 {LoginId1}, sex {Sex}",
            accountId, loginId1, sex);
    }

    public bool TryConsume(int accountId, int loginId1, byte sex)
    {
        if (!_entries.TryRemove(accountId, out var entry))
        {
            _logger.LogInformation(
                "ReturningClientAuth.TryConsume: account {AccountId} — no entry",
                accountId);
            return false;
        }
        if (DateTime.UtcNow - entry.IssuedAt > TimeSpan.FromSeconds(30))
        {
            _logger.LogInformation(
                "ReturningClientAuth.TryConsume: account {AccountId} — entry expired",
                accountId);
            return false;
        }
        var match = entry.LoginId1 == loginId1 && entry.Sex == sex;
        _logger.LogInformation(
            "ReturningClientAuth.TryConsume: account {AccountId} stored=(loginId1={StoredLoginId1}, sex={StoredSex}) packet=(loginId1={PacketLoginId1}, sex={PacketSex}) match={Match}",
            accountId, entry.LoginId1, entry.Sex, loginId1, sex, match);
        return match;
    }

    private readonly record struct Entry(int LoginId1, byte Sex, DateTime IssuedAt);
}
