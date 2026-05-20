using Map.Server.Entities;

namespace Map.Server.Spawn.MobOps;

/// <summary>
/// Top-level mob operations. Canonical entry points for rAthena
/// <c>mob.cpp</c> (6 967 lines, 76 public functions).
///
/// AI + skill use already live in <c>Map.Server.Mob</c>. This
/// service surfaces the rAthena-named lifecycle + spawn helpers
/// that callers reach for: `mob_spawn`, `mob_warpslave`,
/// `mob_setdelayspawn`, `mob_summon_*`, `mob_clone`,
/// `mob_setclass`, etc.
/// </summary>
public interface IMobOpsService
{
    /// <summary>rAthena <c>mob_spawn</c> — instantiate a mob at its spawn point.</summary>
    bool Spawn(MobEntity mob);

    /// <summary>rAthena <c>mob_warpslave</c> — teleport slaves to master.</summary>
    int WarpSlave(MobEntity master, short range);

    /// <summary>rAthena <c>mob_dead</c> — death routine.</summary>
    int Dead(MobEntity mob, Entity? killer, byte type);

    /// <summary>rAthena <c>mob_damage</c> — apply HP loss + track attacker.</summary>
    int Damage(Entity src, MobEntity mob, int damage);

    /// <summary>rAthena <c>mob_heal</c>.</summary>
    int Heal(MobEntity mob, int hp);

    /// <summary>rAthena <c>mob_setclass</c> — class change.</summary>
    int SetClass(MobEntity mob, int newClassId);

    /// <summary>rAthena <c>mob_setdelayspawn</c>.</summary>
    int SetDelaySpawn(MobEntity mob, long ticks);

    /// <summary>rAthena <c>mob_summon_slave</c>.</summary>
    int SummonSlave(MobEntity master, int classId, ushort amount, ushort skillId);

    /// <summary>rAthena <c>mob_clone</c>.</summary>
    int Clone(Entity src, int classId, byte duration);

    /// <summary>rAthena <c>mob_clone_delete</c>.</summary>
    int CloneDelete(MobEntity clone);

    /// <summary>rAthena <c>mob_chat_sub</c>.</summary>
    int ChatSub(MobEntity mob, string text);

    /// <summary>rAthena <c>mob_setdamageimmunity</c>.</summary>
    void SetDamageImmunity(MobEntity mob, bool immune);

    /// <summary>rAthena <c>mob_changestate</c>.</summary>
    int ChangeState(MobEntity mob, byte state, long tick);

    /// <summary>rAthena <c>mob_drop_adjust</c>.</summary>
    int DropAdjust(int chance, ushort dropRate, int min, int max);

    /// <summary>rAthena <c>mob_get_random_id</c>.</summary>
    int GetRandomId(int type, byte flag, int level);

    /// <summary>rAthena <c>mobdb_searchname</c>.</summary>
    int SearchName(string name);

    /// <summary>rAthena <c>mobdb_searchname_array</c>.</summary>
    int SearchNameArray(string namePattern, IList<int> output, int max);

    /// <summary>rAthena <c>mobdb_reload</c>.</summary>
    void Reload();
}
