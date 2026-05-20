namespace Core.Database.Entities;

/// <summary>
/// Pet definition. One row per tameable mob. Seeded from
/// <c>db/re/pet_db.yml</c>.
/// </summary>
public class PetDbEntity
{
    public string MobAegis { get; set; } = string.Empty;
    public string? TameItem { get; set; }
    public string? EggItem { get; set; }
    public string? EquipItem { get; set; }
    public string? FoodItem { get; set; }
    public int? Fullness { get; set; }
    public int? HungerDelay { get; set; }
    public int? IntimacyStart { get; set; }
    public int? IntimacyFed { get; set; }
    public int? IntimacyOverfed { get; set; }
    public int? IntimacyHungry { get; set; }
    public int? IntimacyOwnerDie { get; set; }
    public int? CaptureRate { get; set; }
    public bool? SpecialPerformance { get; set; }
    public int? AttackRate { get; set; }
    public int? RetaliateRate { get; set; }
    public int? ChangeTargetRate { get; set; }
    public bool? AllowAutoFeed { get; set; }
    public string? Script { get; set; }
    public string? SupportScript { get; set; }
}
