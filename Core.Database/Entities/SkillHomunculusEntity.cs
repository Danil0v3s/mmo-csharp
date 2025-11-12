namespace Core.Database.Entities;

public class SkillHomunculusEntity
{
    public int HomunId { get; set; }
    public int Id { get; set; }
    public short Lv { get; set; }
    
    // Navigation properties
    public HomunculusEntity? Homunculus { get; set; }
}

