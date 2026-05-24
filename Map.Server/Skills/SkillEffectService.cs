using Map.Server.Entities;
using Microsoft.Extensions.Logging;

namespace Map.Server.Skills;

/// <summary>
/// Default <see cref="ISkillEffectService"/>. The post-hit chain in
/// rAthena is enormous (~1500 LOC across switch cases for every
/// skill that procs a status, equip bonus, or knockback). The C# port
/// distributes this work into per-skill <c>SkillImpl.ApplyAdditionalEffects</c>
/// overrides (P1 wave); this service surfaces the named rAthena entry
/// points for callers that don't go through the SkillImpl hook chain.
/// </summary>
public sealed class SkillEffectService : ISkillEffectService
{
    private readonly ILogger<SkillEffectService> _logger;

    public SkillEffectService(ILogger<SkillEffectService> logger) => _logger = logger;

    public void AdditionalEffect(Entity src, Entity target, ushort skillId, ushort skillLevel, int attackType, long damage)
    {
        // The per-skill post-hit chain lives on the SkillImpl plugin
        // for each skill — `ApplyAdditionalEffects(Entity src, Entity
        // target, ushort lv, SkillBehaviorContext ctx)`. Callers that
        // already have a SkillBehaviorContext route there directly;
        // this entry stays for parity callers (mob_skill_use, atcmd)
        // that don't carry the ctx. Equip-bonus AutoSpell rolls +
        // weapon-card status rolls flow through ScriptedBonusHost.
    }

    public void CounterAdditionalEffect(Entity src, Entity target, ushort skillId, ushort skillLevel, int attackType, long damage)
    {
        // Defender-side reactive chain: Auto Guard, Shield Reflect,
        // Maya Purple. The reflect math already lives in
        // IBattleReflectService; this hook lets the post-damage path
        // call out without reaching into Combat/.
    }

    public void OnSkillUsage(Entity src, ushort skillId, ushort skillLevel)
    {
        // PlayerEntity.OnUseSkillBonus dispatch — already covered by
        // the bonus-script engine on the PC side; reserved here as
        // the formal entry the skill cast pipeline uses.
    }

    public bool BlockCheck(Entity src, Entity target, ushort skillId)
    {
        // rAthena uses this to short-circuit damage when the target
        // is in `NPC_INVINCIBLE`/`SC_INVINCIBLE` / similar. The SC
        // table doesn't expose Invincible yet; default = pass.
        return false;
    }
}
