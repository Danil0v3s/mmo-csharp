using Map.Server.Entities;
using Map.Server.Session;

namespace Map.Server.Chat;

/// <summary>
/// Routes party / guild / whisper chat lines. Local same-map delivery
/// uses the visibility / session managers; cross-server hands off to
/// the char server via the existing P5 IPC routes (
/// <c>PartyMessage</c> / <c>GuildMessage</c> / <c>InterWhisper</c>).
///
/// Mirrors rAthena <c>party_send_message</c> /
/// <c>guild_send_message</c> / <c>clif_wis_message</c> dispatch.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Send a whisper from <paramref name="sender"/> to the named target.
    /// Local delivery wins; cross-server delivery hands off to char.
    /// Returns the outcome so the caller can shape the rAthena
    /// auto-reply (delivered, target-not-online, blocked).
    /// </summary>
    Task<ChatDeliveryResult> SendWhisperAsync(
        PlayerEntity sender, MapSessionData senderSession,
        string targetName, string message,
        CancellationToken ct = default);

    /// <summary>
    /// Send a party chat line. Hands off to char so all member map
    /// servers receive it (the char→map fan-out is the
    /// <c>PartyMessage</c> IPC). Local members on this map also get
    /// the packet immediately so the speaker doesn't see a round-trip
    /// delay.
    /// </summary>
    Task SendPartyAsync(PlayerEntity sender, string message, CancellationToken ct = default);

    /// <summary>
    /// Send a guild chat line. Same shape as
    /// <see cref="SendPartyAsync"/>, routed through
    /// <c>GuildMessage</c> IPC.
    /// </summary>
    Task SendGuildAsync(PlayerEntity sender, string message, CancellationToken ct = default);
}

public enum ChatDeliveryResult
{
    /// <summary>Local same-map delivery — recipient got the packet.</summary>
    DeliveredLocal,
    /// <summary>Handed off to char server for cross-server routing.</summary>
    DeliveredCrossServer,
    /// <summary>Target isn't online anywhere we can reach.</summary>
    TargetOffline,
    /// <summary>Sender lacks the affiliation (no party, no guild, etc.).</summary>
    NoAffiliation,
}
