using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.ElementalNpc;

/// <summary>
/// EL_ROCK_CRUSHER — Elemental Rock Launcher. Manual port of
/// <c>rathena-fork/src/map/skills/elemental/rocklauncher.cpp</c>.
/// +700 ratio; 50/50 magic hit (this skill) vs weapon hit
/// (EL_ROCK_CRUSHER_ATK at +200). Applies SC_ROCK_CRUSHER at 50%.
/// </summary>
public sealed class RockLauncher : SkillImpl
{
    private readonly ISkillAttackService? _skillAttack;

    public RockLauncher() : base(SkillIds.EL_ROCK_CRUSHER) { }

    public RockLauncher(ISkillAttackService? skillAttack = null) : base(SkillIds.EL_ROCK_CRUSHER)
    {
        _skillAttack = skillAttack;
    }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 700;

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, src, SkillId, skillLevel);
        // 50/50 magic vs weapon — fall back to magic since EL_ROCK_CRUSHER_ATK
        // isn't yet a registered SkillId.
        var asMagic = System.Random.Shared.Next(100) < 50;
        _skillAttack?.SkillAttack(asMagic ? BattleAttackType.Magic : BattleAttackType.Weapon,
            src, src, target, SkillId, skillLevel);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (System.Random.Shared.Next(100) < 50)
            ctx.Sc?.Start(target, StatusType.RockCrusher, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
    }
}
