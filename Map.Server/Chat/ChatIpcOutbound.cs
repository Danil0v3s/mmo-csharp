using Map.Server.Services;

namespace Map.Server.Chat;

/// <summary>
/// Production adapter — folds the three chat IPC routes onto the wider
/// <c>ICharServerIpcService*</c> wrappers. Tests substitute
/// <see cref="IChatIpcOutbound"/> directly to avoid having to stub the
/// full per-service surfaces.
/// </summary>
public sealed class ChatIpcOutbound : IChatIpcOutbound
{
    private readonly ICharServerIpcServiceInter _inter;
    private readonly ICharServerIpcServiceParty _party;
    private readonly ICharServerIpcServiceGuild _guild;

    public ChatIpcOutbound(
        ICharServerIpcServiceInter inter,
        ICharServerIpcServiceParty party,
        ICharServerIpcServiceGuild guild)
    {
        _inter = inter;
        _party = party;
        _guild = guild;
    }

    public async Task<bool> WhisperAsync(
        int senderAccountId, long senderCharacterId,
        string senderName, string targetName, string message,
        CancellationToken ct = default)
    {
        var resp = await _inter.InterWhisperAsync(
            senderAccountId, senderCharacterId, senderName, targetName, message, ct);
        return resp != null;
    }

    public async Task<bool> PartyMessageAsync(int partyId, int senderAccountId, string message, CancellationToken ct = default)
    {
        var resp = await _party.PartyMessageAsync(partyId, senderAccountId, message, ct);
        return resp?.Success == true;
    }

    public async Task<bool> GuildMessageAsync(int guildId, int senderAccountId, string message, CancellationToken ct = default)
    {
        var resp = await _guild.GuildMessageAsync(guildId, senderAccountId, message, ct);
        return resp?.Success == true;
    }
}
