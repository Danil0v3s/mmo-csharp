using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Mercenary;

/// <summary>Default <see cref="IMercenaryService"/>. Entry-point shells; persistence + AI data-pending.</summary>
public sealed class MercenaryService : IMercenaryService
{
    private readonly ILogger<MercenaryService> _logger;
    public MercenaryService(ILogger<MercenaryService> logger) => _logger = logger;

    public bool Create(PlayerEntity master, int classId, int lifetimeMs) => false;
    public bool Dead(PlayerEntity master) => false;
    public int Delete(PlayerEntity master, byte reason) => 0;
    public bool RecvData(PlayerEntity master) => false;
    public void Save(PlayerEntity master) { }
    public int GetCalls(int classId) => 0;
    public void SetCalls(PlayerEntity master, int delta) { }
    public int GetFaith(PlayerEntity master) => 0;
    public void SetFaith(PlayerEntity master, int delta) { }
    public long GetLifetimeMs(PlayerEntity master) => 0;
    public void Heal(PlayerEntity master, int hp, int sp) { }
    public void KillBonus(PlayerEntity master) { }
    public void Kills(PlayerEntity master) { }
    public ushort CheckSkill(PlayerEntity master, ushort skillId) => 0;
    public void ContractInit(PlayerEntity master) { }
    public void ContractStop(PlayerEntity master) { }
}
