using Map.Server.Entities;
using Map.Server.Mob;

namespace Map.Server.Skills.Behaviors.Ninja;

/// <summary>
/// KO_ZANZOU — Illusion Shadow (skill.cpp:KO_ZANZOU arm). Spawns a
/// clone mob (<see cref="MobIds.Zanzou"/>) tagged with
/// <see cref="MobSpecialAi.Zanzou"/> + master link + 5 s lifetime
/// cap. The clone soaks the caster's hate so nearby aggressive mobs
/// retarget it (handled by the Zanzou AI on the mob side).
/// </summary>
public sealed class IllusionShadow : SkillImpl
{
    private const int LifetimeMs = 5_000;

    public IllusionShadow() : base(SkillIds.KO_ZANZOU) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        ctx.MobSpawn?.SpawnWithAi(src.Id, src.MapId, MobIds.Zanzou, src.X, src.Y,
            MobSpecialAi.Zanzou, LifetimeMs);
    }
}
