namespace Core.Database.Entities;

public class SkillCooldownEntity
{
    public int AccountId { get; set; }
    public int CharId { get; set; }
    public ushort Skill { get; set; }
    public long Tick { get; set; }
}

