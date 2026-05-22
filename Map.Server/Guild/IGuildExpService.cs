using Map.Server.Entities;

namespace Map.Server.Guild;

/// <summary>
/// Per-PC guild EXP accumulator. Mirrors rAthena's
/// <c>guild_expcache_db</c> + <c>guild_payexp</c> /
/// <c>guild_getexp</c> / <c>guild_payexp_timer</c> at guild.cpp:1681
/// / :1712 / :647.
///
/// The PC's combat EXP gain pays a tax to the guild (per the guild's
/// position.ExpMode); the accumulated value flushes to the
/// authoritative <see cref="GuildMember.Exp"/> + char-server
/// <c>GuildMemberInfoChange(GMI_EXP)</c> on a one-minute timer.
/// </summary>
public interface IGuildExpService
{
    /// <summary>
    /// rAthena <c>guild_payexp</c> (cpp:1681). PC pays a tax on EXP
    /// gained from combat. Tax rate comes from the PC's position's
    /// <c>exp_mode</c> (0..100); ≥ 100 means "tax everything".
    /// Returns the taxed amount (0 if no tax / not in guild).
    /// </summary>
    long PayExp(PlayerEntity pc, long exp);

    /// <summary>
    /// rAthena <c>guild_getexp</c> (cpp:1712). PC pays a tribute
    /// (full amount, no tax-rate); used by scripts / NPCs to give
    /// guild EXP directly. Returns the amount queued.
    /// </summary>
    long GetExp(PlayerEntity pc, long exp);

    /// <summary>
    /// rAthena <c>guild_payexp_timer_sub</c> (cpp:624) — flush one
    /// PC's accumulated cache to <see cref="GuildEntity.Members"/>[i].Exp
    /// + dispatch <c>GuildMemberInfoChangeAsync(GMI_EXP)</c>. Returns
    /// the flushed amount.
    /// </summary>
    long FlushOne(int charId);

    /// <summary>
    /// rAthena <c>guild_payexp_timer</c> (cpp:647) — minute-tick
    /// flush of every cached entry. Returns the count of entries
    /// that landed exp on the guild side.
    /// </summary>
    int FlushAll();

    /// <summary>Peek the accumulated unflushed exp for a PC (0 if none).</summary>
    long Peek(int charId);

    /// <summary>Test/diagnostic snapshot of the cache.</summary>
    System.Collections.Generic.IReadOnlyDictionary<int, (int GuildId, int AccountId, long Exp)> Snapshot();
}
