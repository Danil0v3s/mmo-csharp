using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Npc;

/// <summary>
/// NPC_BLOODDRAIN — Mirrors
/// <c>rathena-fork/src/map/skills/npc/suckingblood.cpp</c>.
/// Weapon hit (ratio +100*(lv-1)); the dealt damage is added back to
/// the caster as HP heal. NOT an SC_BLOODSUCKER application — that
/// was a port bug found via the T3.1 rAthena parity sweep.
/// </summary>
public sealed class SuckingBlood : WeaponSkillImpl
{
    public SuckingBlood() : base(SkillIds.NPC_BLOODDRAIN) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio + 100 * (skillLevel - 1);

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        base.CastendDamageId(src, target, skillLevel, ctx);
        // status_heal(src, dmg, 0, 0) — heal the caster by the damage
        // dealt. The dealt damage isn't available here in the current
        // C# shape (the value lives in the IDamageService return);
        // approximate as 100 HP per tick for now — TODO: thread the
        // damage value through SkillBehaviorContext.
        switch (src)
        {
            case PlayerEntity p: p.Hp = System.Math.Min(p.MaxHp, p.Hp + 100); break;
            case MobEntity m: m.Hp = System.Math.Min(m.MaxHp, m.Hp + 100); break;
        }
    }
}
