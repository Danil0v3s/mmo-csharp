using Core.Server.IPC;

namespace Map.Server.Services;

public interface ICharServerIpcServiceMail
{
    Task<MailRequestInboxResponse?> MailRequestInboxAsync(
        int accountId,
        long characterId,
        CancellationToken cancellationToken = default);

    Task<MailReadResponse?> MailReadAsync(
        int accountId,
        long characterId,
        long mailId,
        CancellationToken cancellationToken = default);

    Task<MailGetAttachmentResponse?> MailGetAttachmentAsync(
        int accountId,
        long characterId,
        long mailId,
        CancellationToken cancellationToken = default);

    Task<MailDeleteResponse?> MailDeleteAsync(
        int accountId,
        long characterId,
        long mailId,
        CancellationToken cancellationToken = default);

    Task<MailReturnResponse?> MailReturnAsync(
        int accountId,
        long characterId,
        long mailId,
        CancellationToken cancellationToken = default);

    Task<MailSendResponse?> MailSendAsync(
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
        CancellationToken cancellationToken = default);

    Task<MailReceiverCheckResponse?> MailReceiverCheckAsync(
        string receiverName,
        CancellationToken cancellationToken = default);
}
