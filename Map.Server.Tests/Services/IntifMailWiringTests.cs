using Map.Server.Entities;
using Map.Server.Services;
using Map.Server.Services.Intif;
using Microsoft.Extensions.Logging.Abstractions;

namespace Map.Server.Tests.Services;

/// <summary>
/// T5.4a — verifies IntifService.MailSend / MailReturn dispatch
/// onto ICharServerIpcService when wired, and short-circuit to 0
/// when the IPC client is absent (the rAthena pre-inter-server-
/// connect behaviour).
/// </summary>
public class IntifMailWiringTests
{
    [Fact]
    public void MailSend_WithoutCharIpc_ReturnsZero()
    {
        // Default ctor (no IPC) — the rAthena "intif_check_connection"
        // gate equivalent. Returns 0 = not sent.
        var intif = new IntifService(NullLogger<IntifService>.Instance);
        var rc = intif.MailSend(senderCharId: 1, toName: "Bob",
            title: "hi", body: "test", zeny: 0);
        Assert.Equal(0, rc);
    }

    [Fact]
    public void MailSend_WithCharIpc_ReturnsOneAndDispatches()
    {
        var fake = new RecordingCharIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, mailIpc: fake);

        var rc = intif.MailSend(senderCharId: 7, toName: "Alice",
            title: "Hello", body: "ping", zeny: 100);

        Assert.Equal(1, rc);
        Assert.Single(fake.MailSendCalls);
        var call = fake.MailSendCalls[0];
        Assert.Equal(7L, call.SenderCharacterId);
        Assert.Equal("Alice", call.ReceiverName);
        Assert.Equal("Hello", call.Title);
        Assert.Equal("ping", call.Body);
        Assert.Equal(100L, call.Zeny);
    }

    [Fact]
    public void MailReturn_WithCharIpc_Dispatches()
    {
        var fake = new RecordingCharIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, mailIpc: fake);

        var rc = intif.MailReturn(charId: 7, mailId: 42);

        Assert.Equal(1, rc);
        Assert.Single(fake.MailReturnCalls);
        Assert.Equal(7L, fake.MailReturnCalls[0].CharacterId);
        Assert.Equal(42L, fake.MailReturnCalls[0].MailId);
    }

    // ---- T7.6 — inbox close-out ----

    [Fact]
    public void MailRequestInbox_WithCharIpc_Dispatches()
    {
        var fake = new RecordingCharIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, mailIpc: fake);

        Assert.Equal(1, intif.MailRequestInbox(charId: 11, flag: 0));
        Assert.Single(fake.MailRequestInboxCalls);
        Assert.Equal(11L, fake.MailRequestInboxCalls[0].CharacterId);
    }

    [Fact]
    public void MailRequestInbox_WithoutCharIpc_ReturnsZero()
    {
        var intif = new IntifService(NullLogger<IntifService>.Instance);
        Assert.Equal(0, intif.MailRequestInbox(charId: 11, flag: 0));
    }

    [Fact]
    public void MailRead_WithCharIpc_Dispatches()
    {
        var fake = new RecordingCharIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, mailIpc: fake);

        Assert.Equal(1, intif.MailRead(mailId: 33));
        Assert.Single(fake.MailReadCalls);
        Assert.Equal(33L, fake.MailReadCalls[0].MailId);
    }

    [Fact]
    public void MailGetAttach_WithCharIpc_Dispatches()
    {
        var fake = new RecordingCharIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, mailIpc: fake);

        Assert.Equal(1, intif.MailGetAttach(charId: 22, mailId: 44, flag: 0));
        Assert.Single(fake.MailGetAttachmentCalls);
        Assert.Equal(22L, fake.MailGetAttachmentCalls[0].CharacterId);
        Assert.Equal(44L, fake.MailGetAttachmentCalls[0].MailId);
    }

    [Fact]
    public void MailDelete_WithCharIpc_Dispatches()
    {
        var fake = new RecordingCharIpc();
        var intif = new IntifService(NullLogger<IntifService>.Instance, mailIpc: fake);

        Assert.Equal(1, intif.MailDelete(charId: 22, mailId: 99));
        Assert.Single(fake.MailDeleteCalls);
        Assert.Equal(22L, fake.MailDeleteCalls[0].CharacterId);
        Assert.Equal(99L, fake.MailDeleteCalls[0].MailId);
    }

    /// <summary>
    /// Records every Mail* dispatch for assertions. Implements only
    /// the Mail subset of ICharServerIpcService.
    /// </summary>
    private sealed class RecordingCharIpc : ICharServerIpcServiceMail
    {
        public sealed record MailSendCall(int SenderAccountId, long SenderCharacterId,
            string SenderName, int ReceiverAccountId, long ReceiverCharacterId,
            string ReceiverName, string Title, string Body, long Zeny);
        public sealed record MailReturnCall(int AccountId, long CharacterId, long MailId);
        public sealed record MailRequestInboxCall(int AccountId, long CharacterId);
        public sealed record MailReadCall(int AccountId, long CharacterId, long MailId);
        public sealed record MailGetAttachmentCall(int AccountId, long CharacterId, long MailId);
        public sealed record MailDeleteCall(int AccountId, long CharacterId, long MailId);

        public List<MailSendCall> MailSendCalls { get; } = new();
        public List<MailReturnCall> MailReturnCalls { get; } = new();
        public List<MailRequestInboxCall> MailRequestInboxCalls { get; } = new();
        public List<MailReadCall> MailReadCalls { get; } = new();
        public List<MailGetAttachmentCall> MailGetAttachmentCalls { get; } = new();
        public List<MailDeleteCall> MailDeleteCalls { get; } = new();

        public Task<Core.Server.IPC.MailRequestInboxResponse?> MailRequestInboxAsync(
            int accountId, long characterId,
            CancellationToken cancellationToken = default)
        {
            MailRequestInboxCalls.Add(new MailRequestInboxCall(accountId, characterId));
            return Task.FromResult<Core.Server.IPC.MailRequestInboxResponse?>(null);
        }

        public Task<Core.Server.IPC.MailReadResponse?> MailReadAsync(
            int accountId, long characterId, long mailId,
            CancellationToken cancellationToken = default)
        {
            MailReadCalls.Add(new MailReadCall(accountId, characterId, mailId));
            return Task.FromResult<Core.Server.IPC.MailReadResponse?>(null);
        }

        public Task<Core.Server.IPC.MailGetAttachmentResponse?> MailGetAttachmentAsync(
            int accountId, long characterId, long mailId,
            CancellationToken cancellationToken = default)
        {
            MailGetAttachmentCalls.Add(new MailGetAttachmentCall(accountId, characterId, mailId));
            return Task.FromResult<Core.Server.IPC.MailGetAttachmentResponse?>(null);
        }

        public Task<Core.Server.IPC.MailDeleteResponse?> MailDeleteAsync(
            int accountId, long characterId, long mailId,
            CancellationToken cancellationToken = default)
        {
            MailDeleteCalls.Add(new MailDeleteCall(accountId, characterId, mailId));
            return Task.FromResult<Core.Server.IPC.MailDeleteResponse?>(null);
        }

        public Task<Core.Server.IPC.MailReturnResponse?> MailReturnAsync(
            int accountId, long characterId, long mailId,
            CancellationToken cancellationToken = default)
        {
            MailReturnCalls.Add(new MailReturnCall(accountId, characterId, mailId));
            return Task.FromResult<Core.Server.IPC.MailReturnResponse?>(null);
        }

        public Task<Core.Server.IPC.MailSendResponse?> MailSendAsync(
            int senderAccountId, long senderCharacterId, string senderName,
            int receiverAccountId, long receiverCharacterId, string receiverName,
            string title, string body, long zeny, byte[] attachment,
            CancellationToken cancellationToken = default)
        {
            MailSendCalls.Add(new MailSendCall(senderAccountId, senderCharacterId,
                senderName, receiverAccountId, receiverCharacterId, receiverName,
                title, body, zeny));
            return Task.FromResult<Core.Server.IPC.MailSendResponse?>(null);
        }

        public Task<Core.Server.IPC.MailReceiverCheckResponse?> MailReceiverCheckAsync(
            string receiverName, CancellationToken cancellationToken = default)
            => Task.FromResult<Core.Server.IPC.MailReceiverCheckResponse?>(null);
    }
}
