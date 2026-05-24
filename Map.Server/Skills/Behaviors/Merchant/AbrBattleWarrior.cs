using Map.Server.Entities;
using Map.Server.Mob;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MT_SUMMON_ABR_BATTLE_WARIOR — Meister Summon ABR Battle Warrior
/// (skill.cpp:MT_SUMMON_ABR_BATTLE_WARIOR). Spawns
/// <see cref="MobIds.AbrBattleWarrior"/> at the caster's cell with
/// AI_ABR + master link + 60 s lifetime; applies SC on the master.
/// </summary>
public sealed class AbrBattleWarrior : SkillImpl
{
    private const int LifetimeMs = 60_000;

    public AbrBattleWarrior() : base(SkillIds.MT_SUMMON_ABR_BATTLE_WARIOR) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.AbrBattleWarior, val1: skillLevel, 0, 0, 0, durationMs: LifetimeMs, src);
        ctx.MobSpawn?.SpawnWithAi(src.Id, src.MapId, MobIds.AbrBattleWarrior, src.X, src.Y,
            MobSpecialAi.Abr, LifetimeMs);
    }
}
