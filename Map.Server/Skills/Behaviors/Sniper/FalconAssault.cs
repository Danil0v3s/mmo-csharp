using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Sniper;

/// <summary>
/// SN_FALCONASSAULT — Sniper Falcon Assault. Mirrors
/// <c>rathena-fork/src/map/skills/sniper/falconassault.cpp</c>.
///
/// Falcon-driven single hit: damage = (250 + 150*lv) % base
/// Blitz Beat output. Requires falcon equipped.
/// </summary>
public sealed class FalconAssault : SkillImpl
{
    public FalconAssault() : base(SkillIds.SN_FALCONASSAULT) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // Blitz-style base + Sniper scaling (250 + 150*lv)%.
        var blitzBase = (src.Stats.Dex * src.Stats.IntStat) / 10 + src.Level + src.Stats.IntStat;
        var rate = 250 + 150 * skillLevel;
        var dmg = Math.Max(1, blitzBase * rate / 100);
        ctx.Damage.ApplyDamage(target, dmg, src);
    }
}
