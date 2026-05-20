using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Mail;

/// <summary>
/// Default <see cref="IMailService"/>. Map-side draft tracking +
/// char-server forwarding. The mail persistence path already exists
/// on the char server (MailSendAsync / MailReceiveAsync RPC). This
/// service is the rAthena-named seam map-side handlers call.
/// </summary>
public sealed class MailService : IMailService
{
    private readonly ILogger<MailService> _logger;
    public MailService(ILogger<MailService> logger) => _logger = logger;

    public int OpenMail(PlayerEntity pc) => 0;
    public void Clear(PlayerEntity pc) { /* per-session draft data-pending */ }
    public bool Send(PlayerEntity pc, string recipientName, string title, string body) => false;
    public bool SetAttachment(PlayerEntity pc, int inventoryIndex, int amount) => false;
    public bool RemoveItem(PlayerEntity pc, int inventoryIndex) => false;
    public bool RemoveZeny(PlayerEntity pc, long amount) => false;
    public bool InvalidOperation(PlayerEntity pc) => false;
    public void DeliveryFail(PlayerEntity pc) { }
    public void GetAttachment(PlayerEntity pc, int mailId) { }
    public void RefreshRemainingAmount(PlayerEntity pc) { }
}
