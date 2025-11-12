namespace Core.Database.Entities;

public class MailEntity
{
    public long Id { get; set; }
    public string SendName { get; set; } = string.Empty;
    public int SendId { get; set; }
    public string DestName { get; set; } = string.Empty;
    public int DestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public uint Time { get; set; }
    public short Status { get; set; }
    public uint Zeny { get; set; }
    public short Type { get; set; }
    
    // Navigation properties
    public ICollection<MailAttachmentEntity> Attachments { get; set; } = new List<MailAttachmentEntity>();
}

