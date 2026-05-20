using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Spawn.MobOps;

/// <summary>Default <see cref="IMobOpsService"/>. Mostly shells — primary mob lifecycle lives in Mob/.</summary>
public sealed class MobOpsService : IMobOpsService
{
    private readonly ILogger<MobOpsService> _logger;
    public MobOpsService(ILogger<MobOpsService> logger) => _logger = logger;

    public bool Spawn(MobEntity mob) => true;
    public int WarpSlave(MobEntity master, short range) => 0;
    public int Dead(MobEntity mob, Entity? killer, byte type) => 0;
    public int Damage(Entity src, MobEntity mob, int damage) => 0;
    public int Heal(MobEntity mob, int hp) { mob.Hp = Math.Min(mob.MaxHp, mob.Hp + hp); return hp; }
    public int SetClass(MobEntity mob, int newClassId) => 0;
    public int SetDelaySpawn(MobEntity mob, long ticks) => 0;
    public int SummonSlave(MobEntity master, int classId, ushort amount, ushort skillId) => 0;
    public int Clone(Entity src, int classId, byte duration) => 0;
    public int CloneDelete(MobEntity clone) => 0;
    public int ChatSub(MobEntity mob, string text) => 0;
    public void SetDamageImmunity(MobEntity mob, bool immune) { }
    public int ChangeState(MobEntity mob, byte state, long tick) => 0;
    public int DropAdjust(int chance, ushort dropRate, int min, int max) => Math.Clamp(chance * dropRate / 10000, min, max);
    public int GetRandomId(int type, byte flag, int level) => 0;
    public int SearchName(string name) => 0;
    public int SearchNameArray(string namePattern, IList<int> output, int max) => 0;
    public void Reload() { }
}
