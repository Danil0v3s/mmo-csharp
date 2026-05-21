using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_TIGERCANNON — Sura Tiger Cannon. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/tigercannon.cpp</c>.
///
/// <para>HP/SP-consuming AoE. Consumes <c>(10 + 2*lv) % MaxHP</c>
/// and <c>(5 + lv) % MaxSP</c>; damage ratio is half of that sum
/// (or full sum when chained from Fallen Empire combo).</para>
///
/// <para>The combo-bonus (SC_COMBO with val1 = SR_FALLENEMPIRE,
/// and not under SC_FLASHCOMBO) doubles the damage; that branch
/// requires SC reads from CalculateSkillRatio which our hook
/// doesn't support yet. The C# port lands the non-combo ratio.</para>
/// </summary>
public sealed class TigerCannon : WeaponSkillImpl
{
    public TigerCannon() : base(SkillIds.SR_TIGERCANNON) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        // rAthena: hp = MaxHP * (10 + 2*lv) / 100;  sp = MaxSP * (5 + lv) / 100;
        var hp = (int)(src.Stats.MaxHp * (10 + 2 * skillLevel) / 100);
        var sp = (int)(src.Stats.MaxSp * (5 + skillLevel) / 100);
        // Non-combo path: (hp + sp) / 4. Combo path would be / 2 (TODO).
        return baseRatio + (-100 + (hp + sp) / 4);
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Self-centered cast → dispatch as damage on src (rAthena fork pattern).
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        CastendDamageId(src, src, skillLevel, ctx);
    }
}
