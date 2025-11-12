namespace Core.Database.Entities;

public class SkillCooldownHomunculusEntity
{
    public int HomunId { get; set; }
    public ushort Skill { get; set; }
    public long Tick { get; set; }
}

