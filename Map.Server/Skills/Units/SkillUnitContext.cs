using Map.Server.Combat;
using Map.Server.Status;

namespace Map.Server.Skills.Units;

/// <summary>
/// Default <see cref="ISkillUnitContext"/> — straight constructor-injection
/// over the underlying services. One instance is shared by the
/// <see cref="SkillUnitService"/> across every tick callback, so it's a
/// singleton in DI.
/// </summary>
public sealed class SkillUnitContext : ISkillUnitContext
{
    public IDamageService Damage { get; }
    public IStatusChangeService? Sc { get; }
    public ISkillClientService? Client { get; }

    public SkillUnitContext(
        IDamageService damage,
        IStatusChangeService? sc = null,
        ISkillClientService? client = null)
    {
        Damage = damage;
        Sc = sc;
        Client = client;
    }
}
