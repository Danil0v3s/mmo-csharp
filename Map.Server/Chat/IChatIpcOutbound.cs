namespace Map.Server.Chat;

/// <summary>
/// Narrow outbound surface for chat IPC. The full
/// <c>ICharServerIpcService*</c> wrappers carry dozens of methods for
/// every party/guild/inter op; chat only needs three. Stubbing those
/// wider interfaces in tests is unmanageable — this narrow contract is
/// what <see cref="ChatService"/> depends on, and
/// <see cref="ChatIpcOutbound"/> is the production adapter that calls
/// through to the full wrappers.
/// </summary>
public interface IChatIpcOutbound
{
    Task<bool> WhisperAsync(int senderAccountId, long senderCharacterId,
        string senderName, string targetName, string message,
        CancellationToken ct = default);

    Task<bool> PartyMessageAsync(int partyId, int senderAccountId, string message,
        CancellationToken ct = default);

    Task<bool> GuildMessageAsync(int guildId, int senderAccountId, string message,
        CancellationToken ct = default);
}
