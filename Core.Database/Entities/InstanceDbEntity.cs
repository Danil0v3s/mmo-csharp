namespace Core.Database.Entities;

/// <summary>Instance / dungeon definition.</summary>
public class InstanceDbEntity
{
    public uint InstanceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TimeLimit { get; set; }
    public int IdleTimeout { get; set; }
    public string? EnterMap { get; set; }
    public int? EnterX { get; set; }
    public int? EnterY { get; set; }
    public string? AdditionalMaps { get; set; }
}
