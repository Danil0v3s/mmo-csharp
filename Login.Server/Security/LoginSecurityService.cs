using System.Net;
using Core.Database.Context;
using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Login.Server.Security;

internal sealed class LoginSecurityService(
    ILogger<LoginSecurityService> logger,
    IServiceScopeFactory scopeFactory,
    LoginServerConfiguration configuration
) : ILoginSecurityService
{
    public async Task<bool> IsIpBannedAsync(IPAddress ip, CancellationToken cancellationToken = default)
    {
        if (!configuration.IpBan)
        {
            return false;
        }

        var patterns = BuildIpPatterns(ip);
        var now = DateTime.UtcNow;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            return await db.IpBanLists
                .AsNoTracking()
                .AnyAsync(row => row.RTime > now && patterns.Contains(row.List), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to check ipban status for {Ip}; denying connection defensively", ip);
            return true;
        }
    }

    public async Task LogLoginAttemptAsync(
        IPAddress ip,
        string username,
        int resultCode,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.LogLogin)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO `loginlog`(`time`,`ip`,`user`,`rcode`,`log`)
               VALUES ({DateTime.UtcNow}, {ip.ToString()}, {username}, {(sbyte)resultCode}, {message})",
            cancellationToken);
    }

    public async Task EnforceDynamicPasswordFailureBanAsync(IPAddress ip, CancellationToken cancellationToken = default)
    {
        if (!configuration.IpBan || !configuration.DynamicPassFailureBan)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        var now = DateTime.UtcNow;
        var since = now.AddMinutes(-configuration.DynamicPassFailureBanInterval);
        var ipString = ip.ToString();

        var failures = await db.LoginLogs
            .AsNoTracking()
            .CountAsync(row =>
                row.Ip == ipString &&
                (row.RCode == 0 || row.RCode == 1) &&
                row.Time > since, cancellationToken);

        if (failures < configuration.DynamicPassFailureBanLimit)
        {
            return;
        }

        var prefixPattern = BuildPrefixPattern24(ip);
        var alreadyBanned = await db.IpBanLists
            .AsNoTracking()
            .AnyAsync(row => row.List == prefixPattern && row.RTime > now, cancellationToken);

        if (alreadyBanned)
        {
            return;
        }

        db.IpBanLists.Add(new IpBanListEntity
        {
            List = prefixPattern,
            BTime = now,
            RTime = now.AddMinutes(configuration.DynamicPassFailureBanDuration),
            Reason = "Password error ban"
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Applied dynamic IP ban for {Pattern}", prefixPattern);
    }

    public async Task CleanupExpiredIpBansAsync(CancellationToken cancellationToken = default)
    {
        if (!configuration.IpBan)
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        var now = DateTime.UtcNow;
        var expired = await db.IpBanLists.Where(row => row.RTime <= now).ToListAsync(cancellationToken);
        if (expired.Count == 0)
        {
            return;
        }

        db.IpBanLists.RemoveRange(expired);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string[] BuildIpPatterns(IPAddress ip)
    {
        var octets = ip.ToString().Split('.');
        if (octets.Length != 4)
        {
            return [];
        }

        return
        [
            $"{octets[0]}.*.*.*",
            $"{octets[0]}.{octets[1]}.*.*",
            $"{octets[0]}.{octets[1]}.{octets[2]}.*",
            $"{octets[0]}.{octets[1]}.{octets[2]}.{octets[3]}"
        ];
    }

    private static string BuildPrefixPattern24(IPAddress ip)
    {
        var octets = ip.ToString().Split('.');
        if (octets.Length != 4)
        {
            return ip.ToString();
        }

        return $"{octets[0]}.{octets[1]}.{octets[2]}.*";
    }
}
