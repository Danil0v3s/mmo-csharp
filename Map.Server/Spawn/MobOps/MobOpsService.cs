using Map.Server.Entities;
using Map.Server.Mob;
using Microsoft.Extensions.Logging;

namespace Map.Server.Spawn.MobOps;

/// <summary>Default <see cref="IMobOpsService"/>. Mostly shells — primary mob lifecycle lives in Mob/.</summary>
public sealed class MobOpsService : IMobOpsService
{
    private readonly ILogger<MobOpsService> _logger;
    private readonly IMobDb? _mobDb;

    public MobOpsService(ILogger<MobOpsService> logger, IMobDb? mobDb = null)
    {
        _logger = logger;
        _mobDb = mobDb;
    }

    public bool Spawn(MobEntity mob) => true;
    public int WarpSlave(MobEntity master, short range) => 0;
    public int Dead(MobEntity mob, Entity? killer, byte type) => 0;
    public int Damage(Entity src, MobEntity mob, int damage) => 0;
    public int Heal(MobEntity mob, int hp) { mob.Hp = Math.Min(mob.MaxHp, mob.Hp + hp); return hp; }

    /// <summary>
    /// rAthena <c>mob_class_change</c> (mob.cpp ~4180) — mutate this
    /// mob's class id, name, and DbEntry in place, then recompute
    /// stats from the new entry. Returns 1 on success, 0 if the
    /// requested class isn't in the loaded mob_db (caller stays on
    /// the old class).
    ///
    /// <para>The caller is responsible for re-broadcasting the
    /// appearance change (vanish + standentry pair to AOI). This
    /// method's job is the in-memory mutation only — matches the
    /// rAthena split where <c>mob_class_change</c> calls
    /// <c>clif_clearunit_area</c>+<c>clif_spawn</c> from the caller
    /// site.</para>
    /// </summary>
    public int SetClass(MobEntity mob, int newClassId)
    {
        if (mob.ClassId == newClassId) return 0;
        var entry = _mobDb?.Get(newClassId);
        if (entry == null)
        {
            _logger.LogWarning("mob_class_change: unknown class {Class} (target stays on {OldClass})",
                newClassId, mob.ClassId);
            return 0;
        }
        mob.SetClass(newClassId, entry.Name, entry);
        // Recompute MaxHp from the new entry; preserve current Hp ratio.
        var oldMax = mob.MaxHp > 0 ? mob.MaxHp : 1;
        var hpRatio = mob.Hp * 100 / oldMax;
        mob.MaxHp = entry.Hp;
        mob.Hp = Math.Max(1, entry.Hp * hpRatio / 100);
        return 1;
    }
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
