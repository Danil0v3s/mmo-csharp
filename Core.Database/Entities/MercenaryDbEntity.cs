namespace Core.Database.Entities;

/// <summary>Mercenary class definition.</summary>
public class MercenaryDbEntity
{
    public uint MercId { get; set; }
    public string? AegisName { get; set; }
    public string? Name { get; set; }
    public int? Level { get; set; }
    public int? Hp { get; set; }
    public int? Sp { get; set; }
    public int? Attack { get; set; }
    public int? Attack2 { get; set; }
    public int? Defense { get; set; }
    public int? MagicDefense { get; set; }
    public int? Str { get; set; }
    public int? Agi { get; set; }
    public int? Vit { get; set; }
    public int? Intel { get; set; }
    public int? Dex { get; set; }
    public int? Luk { get; set; }
    public int? AttackRange { get; set; }
    public int? SkillRange { get; set; }
    public int? ChaseRange { get; set; }
    public string? Size { get; set; }
    public string? Race { get; set; }
    public string? Element { get; set; }
    public int? EleLevel { get; set; }
    public int? WalkSpeed { get; set; }
    public int? AttackDelay { get; set; }
    public int? AttackMotion { get; set; }
    public int? DamageMotion { get; set; }
}
