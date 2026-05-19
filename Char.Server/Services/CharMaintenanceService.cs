using Core.Database.Context;
using Core.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Char.Server.Services;

/// <summary>
/// Periodic char-server housekeeping. Mirrors the four rAthena
/// background timers wired in <c>char.cpp:char_set_defaults</c>:
///
/// <list type="bullet">
///   <item><c>mail_return_timer</c> (int_mail.cpp:317) — every 1 min;
///         returns <see cref="MailType.InboxNormal"/> mails older than
///         <c>mail_return_days</c> to their sender.</item>
///   <item><c>mail_delete_timer</c> (int_mail.cpp:321) — every 1 min;
///         deletes <see cref="MailType.InboxReturned"/> mails older than
///         <c>mail_delete_days</c>.</item>
///   <item><c>char_clan_member_cleanup</c> (char.cpp:2216) — every 60 min;
///         sets <c>clan_id = 0</c> for offline chars whose
///         <c>last_login</c> is older than
///         <c>clan_remove_inactive_days</c>.</item>
///   <item><c>char_online_data_cleanup</c> (char.cpp:2190) — every 10 min;
///         flips <c>online = 0</c> for chars whose <c>last_map</c> is
///         not hosted by any currently-registered map server.</item>
/// </list>
///
/// Each tick method is idempotent and side-effect-bounded (one DB scope,
/// one transaction). Cadence is enforced by stored deadline
/// timestamps; the caller (game loop) just invokes <see cref="TickAsync"/>
/// every game tick.
/// </summary>
public sealed class CharMaintenanceService : ICharMaintenanceService
{
    /// <summary>rAthena <c>mail_message.type</c> enum mirror.</summary>
    private enum MailType : short { InboxNormal = 0, InboxReturned = 1 }
    /// <summary>rAthena <c>mail_status</c> enum mirror (subset).</summary>
    private const short MailStatusNew = 0;

    private readonly IServiceScopeFactory _scopes;
    private readonly CharServerConfiguration _config;
    private readonly IMapServerRegistryService _mapRegistry;
    private readonly ILogger<CharMaintenanceService> _logger;

    private DateTime _nextMailReturnUtc = DateTime.MinValue;
    private DateTime _nextMailDeleteUtc = DateTime.MinValue;
    private DateTime _nextClanCleanupUtc = DateTime.MinValue;
    private DateTime _nextOnlineCleanupUtc = DateTime.MinValue;

    public CharMaintenanceService(
        IServiceScopeFactory scopes,
        CharServerConfiguration config,
        IMapServerRegistryService mapRegistry,
        ILogger<CharMaintenanceService> logger)
    {
        _scopes = scopes;
        _config = config;
        _mapRegistry = mapRegistry;
        _logger = logger;
    }

    public async Task TickAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        if (now >= _nextMailReturnUtc)
        {
            _nextMailReturnUtc = now.AddMinutes(1);
            await TickMailReturnAsync(ct);
        }
        if (now >= _nextMailDeleteUtc)
        {
            _nextMailDeleteUtc = now.AddMinutes(1);
            await TickMailDeleteAsync(ct);
        }
        if (now >= _nextClanCleanupUtc)
        {
            _nextClanCleanupUtc = now.AddMinutes(60);
            await TickClanCleanupAsync(ct);
        }
        if (now >= _nextOnlineCleanupUtc)
        {
            _nextOnlineCleanupUtc = now.AddMinutes(10);
            await TickOnlineCleanupAsync(ct);
        }
    }

    // --- internal tick paths (also exposed for tests via interface) ---

    public async Task<int> RunMailReturnTickAsync(CancellationToken ct = default)
        => await TickMailReturnAsync(ct);

    public async Task<int> RunMailDeleteTickAsync(CancellationToken ct = default)
        => await TickMailDeleteAsync(ct);

    public async Task<int> RunClanCleanupTickAsync(CancellationToken ct = default)
        => await TickClanCleanupAsync(ct);

    public async Task<int> RunOnlineCleanupTickAsync(CancellationToken ct = default)
        => await TickOnlineCleanupAsync(ct);

    private async Task<int> TickMailReturnAsync(CancellationToken ct)
    {
        if (_config.MailReturnDays <= 0) return 0;

        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        var cutoffUnix = (uint)DateTimeOffset.UtcNow
            .AddDays(-_config.MailReturnDays).ToUnixTimeSeconds();

        // int_mail.cpp:mail_timer_sub: pulls all NORMAL mails older than
        // mail_return_days, then for each runs mapif_Mail_return which
        // (1) deletes the row, (2) optionally skips empty mails when
        // mail_return_empty is false, (3) swaps sender/dest, prefixes
        // title with "RE:", flips type to INBOX_RETURNED, persists new row.
        var stale = await db.Mails
            .Where(m => m.Type == (short)MailType.InboxNormal && m.Time <= cutoffUnix)
            .Include(m => m.Attachments)
            .ToListAsync(ct);
        if (stale.Count == 0) return 0;

        var processed = 0;
        foreach (var msg in stale)
        {
            // Don't return mails from server (sender_id 0).
            if (msg.SendId == 0)
            {
                db.Mails.Remove(msg);
                processed++;
                continue;
            }
            // mail_return_empty: server-initiated returns skip mails with
            // no attachments AND no zeny.
            if (_config.MailReturnEmpty == 0 && msg.Zeny == 0 && msg.Attachments.Count == 0)
            {
                continue;
            }

            // Swap and rebrand as RE:.
            var newTitle = "RE:" + msg.Title;
            if (newTitle.Length > 40) newTitle = newTitle[..40];
            (msg.SendId, msg.DestId) = (msg.DestId, msg.SendId);
            (msg.SendName, msg.DestName) = (msg.DestName, msg.SendName);
            msg.Title = newTitle;
            msg.Status = MailStatusNew;
            msg.Type = (short)MailType.InboxReturned;
            msg.Time = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            processed++;
        }

        await db.SaveChangesAsync(ct);
        if (processed > 0)
        {
            _logger.LogInformation(
                "Mail return tick: processed {Count} mails (cutoff {Days}d)",
                processed, _config.MailReturnDays);
        }
        return processed;
    }

    private async Task<int> TickMailDeleteAsync(CancellationToken ct)
    {
        if (_config.MailDeleteDays <= 0) return 0;

        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        var cutoffUnix = (uint)DateTimeOffset.UtcNow
            .AddDays(-_config.MailDeleteDays).ToUnixTimeSeconds();

        var stale = await db.Mails
            .Where(m => m.Type == (short)MailType.InboxReturned && m.Time <= cutoffUnix)
            .ToListAsync(ct);
        if (stale.Count == 0) return 0;

        db.Mails.RemoveRange(stale);
        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Mail delete tick: removed {Count} returned mails (cutoff {Days}d)",
            stale.Count, _config.MailDeleteDays);
        return stale.Count;
    }

    private async Task<int> TickClanCleanupAsync(CancellationToken ct)
    {
        if (_config.ClanRemoveInactiveDays <= 0) return 0;

        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
        var cutoff = DateTime.UtcNow.AddDays(-_config.ClanRemoveInactiveDays);

        // rAthena char.cpp:char_clan_member_cleanup:
        //   UPDATE char SET clan_id=0 WHERE online=0 AND clan_id<>0
        //   AND last_login IS NOT NULL AND last_login <= NOW() - INTERVAL days DAY
        var inactive = await db.Characters
            .Where(c => c.Online == 0 && c.ClanId != 0
                && c.LastLogin != null && c.LastLogin <= cutoff)
            .ToListAsync(ct);
        if (inactive.Count == 0) return 0;

        foreach (var ch in inactive) ch.ClanId = 0;
        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Clan inactive cleanup: removed {Count} chars from clans (cutoff {Days}d)",
            inactive.Count, _config.ClanRemoveInactiveDays);
        return inactive.Count;
    }

    private async Task<int> TickOnlineCleanupAsync(CancellationToken ct)
    {
        // rAthena char.cpp:char_online_data_cleanup keys off an in-memory
        // online_char_db where each row carries the map-server id; entries
        // marked server==-2 (set when a map server's gRPC channel drops)
        // get GC'd here. The C# port persists Online directly on the char
        // row and doesn't track per-char map-server id, so we infer
        // "orphaned" from the char's LastMap not appearing in any
        // registered map server's map list.
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();

        var onlineChars = await db.Characters
            .Where(c => c.Online == 1)
            .Select(c => new { c.CharId, c.LastMap })
            .ToListAsync(ct);
        if (onlineChars.Count == 0) return 0;

        var orphanIds = onlineChars
            .Where(c => string.IsNullOrEmpty(c.LastMap)
                || !_mapRegistry.ContainsMap(c.LastMap!))
            .Select(c => c.CharId)
            .ToList();
        if (orphanIds.Count == 0) return 0;

        var rows = await db.Characters
            .Where(c => orphanIds.Contains(c.CharId))
            .ToListAsync(ct);
        foreach (var ch in rows) ch.Online = 0;
        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Online cleanup: flipped {Count} chars to offline (last_map not on any registered map server)",
            rows.Count);
        return rows.Count;
    }
}

public interface ICharMaintenanceService
{
    /// <summary>Game-loop entry point — fires due ticks based on stored deadlines.</summary>
    Task TickAsync(CancellationToken ct);

    /// <summary>Force a mail-return pass regardless of deadline (tests).</summary>
    Task<int> RunMailReturnTickAsync(CancellationToken ct = default);

    /// <summary>Force a mail-delete pass regardless of deadline (tests).</summary>
    Task<int> RunMailDeleteTickAsync(CancellationToken ct = default);

    /// <summary>Force a clan-cleanup pass regardless of deadline (tests).</summary>
    Task<int> RunClanCleanupTickAsync(CancellationToken ct = default);

    /// <summary>Force an online-orphan pass regardless of deadline (tests).</summary>
    Task<int> RunOnlineCleanupTickAsync(CancellationToken ct = default);
}
