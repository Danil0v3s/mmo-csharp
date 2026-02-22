using System.Net;

namespace Login.Server.Security;

public interface ILoginSecurityService
{
    Task<bool> IsIpBannedAsync(IPAddress ip, CancellationToken cancellationToken = default);
    Task LogLoginAttemptAsync(IPAddress ip, string username, int resultCode, string message, CancellationToken cancellationToken = default);
    Task EnforceDynamicPasswordFailureBanAsync(IPAddress ip, CancellationToken cancellationToken = default);
    Task CleanupExpiredIpBansAsync(CancellationToken cancellationToken = default);
}
