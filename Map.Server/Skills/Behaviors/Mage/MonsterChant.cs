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
        // TODO: spawn a random mob at (src.X, src.Y) via IMobSpawnService.SpawnOnce.
    }
}
