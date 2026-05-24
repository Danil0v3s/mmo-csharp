using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_SUMMONMONSTER — Sage Summon Monster (Hocus Pocus). Spawns one
/// random monster at the caster's cell. <c>mob_once_spawn</c>
/// equivalent isn't wired through SkillBehaviorContext yet.
/// </summary>
public sealed class MonsterChant : SkillImpl
{
    public MonsterChant() : base(SkillIds.SA_SUMMONMONSTER) { }
    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // Resolve map name from the caster's hashed MapId, then summon one random mob there.
        if (ctx.World == null || ctx.MobSpawn == null || ctx.MobOps == null) return;
        string? mapName = null;
        foreach (var m in ctx.World.All)
        {
            if ((uint)m.Name.GetHashCode() == src.MapId) { mapName = m.Name; break; }
        }
        if (mapName == null) return;
        // rAthena uses mob_get_random_id(MOBG_BRANCH_OF_DEAD_TREE,...); we approximate by
        // calling the unfiltered helper (type=0) — proper mob group lookup is Deferred until
        // the mob-group YAML is wired into IMobOpsService.
        var classId = ctx.MobOps.GetRandomId(0, 0, 1);
        if (classId <= 0) return;
        ctx.MobSpawn.SpawnAt(mapName, classId, src.X, src.Y);
    }
}
