using Map.Server.Combat;
using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Swordman;

/// <summary>
/// RK_WINDCUTTER — Rune Knight Wind Cutter. Manual port of
/// <c>rathena-fork/src/map/skills/swordman/windcutter.cpp</c>.
///
/// <para>Ratio is weapon-typed (rAthena <c>sd-&gt;weapontype1</c>):
/// <list type="bullet">
///   <item>W_2HSWORD (3) → <c>+(-100 + 250*lv)</c></item>
///   <item>W_1HSPEAR (4) / W_2HSPEAR (5) → <c>+(-100 + 400*lv)</c></item>
///   <item>anything else → <c>+(-100 + 300*lv)</c></item>
/// </list>
/// 2H-sword wielders also fire a double-hit (matching rAthena
/// <c>dmg.div_ = 2</c>). 1H/2H-spear wielders flag the attack
/// <c>BF_LONG</c> in rAthena; we no-op that flag since the C# damage
/// pipeline doesn't model range mask.</para>
/// </summary>
public sealed class WindCutter : RecursiveDamageSplashSkillImpl
{
    private const int W_2HSWORD = 3;
    private const int W_1HSPEAR = 4;
    private const int W_2HSPEAR = 5;

    public WindCutter() : base(SkillIds.RK_WINDCUTTER) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        var weapon = (src is PlayerEntity sd) ? sd.WeaponType : -1;
        var perLevel = weapon switch
        {
            W_2HSWORD => 250,
            W_1HSPEAR or W_2HSPEAR => 400,
            _ => 300,
        };
        return baseRatio + (-100 + perLevel * skillLevel);
    }

    public override void ModifyDamageData(ref BattleDamage dmg, Entity src, Entity target, ushort skillLevel)
    {
        if (src is PlayerEntity pc && pc.WeaponType == W_2HSWORD)
            dmg.Hits = 2;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
}
