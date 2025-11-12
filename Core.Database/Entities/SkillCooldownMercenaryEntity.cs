namespace Core.Database.Entities;

public class SkillCooldownMercenaryEntity
{
    public int MerId { get; set; }
    public ushort Skill { get; set; }
    public long Tick { get; set; }
}

