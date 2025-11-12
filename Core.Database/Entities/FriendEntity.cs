namespace Core.Database.Entities;

public class FriendEntity
{
    public int CharId { get; set; }
    public int FriendId { get; set; }
    
    // Navigation properties
    public CharEntity? Character { get; set; }
}

