using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Merchant;

/// <summary>
/// MT_SUMMON_ABR_BATTLE_WARIOR — Meister Summon ABR Battle Warrior.
/// Manual port of <c>rathena-fork/src/map/skills/merchant/abrbattlewarior.cpp</c>.
/// Spawns an ABR pet (mob_once_spawn TODO); applies SC_ABR_BATTLE_WARIOR
/// to the caster.
/// </summary>
public sealed class AbrBattleWarrior : SkillImpl
{
    public AbrBattleWarrior() : base(SkillIds.MT_SUMMON_ABR_BATTLE_WARIOR) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.Sc?.Start(target, StatusType.AbrBattleWarior, val1: skillLevel, 0, 0, 0, durationMs: 60_000, src);
        // Deferred: MOBID_ABR_BATTLE_WARIOR ABR pet + master-id binding + delete-
        // timer aren't surfaced through IMobSpawnService.SpawnAt.
    }
}
