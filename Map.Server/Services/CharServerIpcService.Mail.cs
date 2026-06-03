using Core.Server.IPC;

namespace Map.Server.Services;

public partial class CharServerIpcService
{
    public async Task<MailRequestInboxResponse?> MailRequestInboxAsync(
        int accountId,
        long characterId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.MailRequestInboxAsync(new MailRequestInboxRequest
        {
            AccountId = accountId,
            CharacterId = characterId
        }, cancellationToken: cancellationToken);
    }

    public async Task<MailReadResponse?> MailReadAsync(
        int accountId,
        long characterId,
        long mailId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.MailReadAsync(new MailReadRequest
        {
            AccountId = accountId,
            CharacterId = characterId,
            MailId = mailId
        }, cancellationToken: cancellationToken);
    }

    public async Task<MailGetAttachmentResponse?> MailGetAttachmentAsync(
        int accountId,
        long characterId,
        long mailId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.MailGetAttachmentAsync(new MailGetAttachmentRequest
        {
            AccountId = accountId,
            CharacterId = characterId,
            MailId = mailId
        }, cancellationToken: cancellationToken);
    }

    public async Task<MailDeleteResponse?> MailDeleteAsync(
        int accountId,
        long characterId,
        long mailId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.MailDeleteAsync(new MailDeleteRequest
        {
            AccountId = accountId,
            CharacterId = characterId,
            MailId = mailId
        }, cancellationToken: cancellationToken);
    }

    public async Task<MailReturnResponse?> MailReturnAsync(
        int accountId,
        long characterId,
        long mailId,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.MailReturnAsync(new MailReturnRequest
        {
            AccountId = accountId,
            CharacterId = characterId,
            MailId = mailId
        }, cancellationToken: cancellationToken);
    }

    public async Task<MailSendResponse?> MailSendAsync(
        int senderAccountId,
        long senderCharacterId,
        string senderName,
        int receiverAccountId,
        long receiverCharacterId,
        string receiverName,
        string title,
        string body,
        long zeny,
        byte[] attachment,
        IReadOnlyList<MailAttachmentItem>? items = null,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        var request = new MailSendRequest
        {
            SenderAccountId = senderAccountId,
            SenderCharacterId = senderCharacterId,
            SenderName = senderName ?? string.Empty,
            ReceiverAccountId = receiverAccountId,
            ReceiverCharacterId = receiverCharacterId,
            ReceiverName = receiverName ?? string.Empty,
            Title = title ?? string.Empty,
            Body = body ?? string.Empty,
            Zeny = zeny,
            Attachment = Google.Protobuf.ByteString.CopyFrom(attachment ?? Array.Empty<byte>())
        };
        // FEATURE-05 — the char side persists from the structured items (it ignores the legacy
        // attachment bytes), so the attachment rides this repeated field.
        if (items != null) request.Items.AddRange(items);
        return await client.MailSendAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<MailReceiverCheckResponse?> MailReceiverCheckAsync(
        string receiverName,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient();
        if (client == null) return null;
        return await client.MailReceiverCheckAsync(new MailReceiverCheckRequest
        {
            ReceiverName = receiverName ?? string.Empty
        }, cancellationToken: cancellationToken);
    }
}
