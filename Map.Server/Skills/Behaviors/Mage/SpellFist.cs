using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SO_SPELLFIST — Sorcerer Spell Fist. Manual port of
/// <c>rathena-fork/src/map/skills/mage/spellfist.cpp</c>.
///
/// <para>Cancels the caster's currently queued bolt spell and replaces
/// the next basic attacks with a magic-charged punch. Reads
/// <c>sd-&gt;skill_id_old</c> / <c>skill_lv_old</c> set during the
/// preceding bolt cast — that bookkeeping isn't wired in our skill
/// engine yet, so the SC starts with the bolt slot zeroed out.</para>
/// </summary>
public sealed class SpellFist : SkillImpl
{
    public SpellFist() : base(SkillIds.SO_SPELLFIST) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // Deferred: skill_id_old / skill_lv_old bookkeeping isn't tracked per session yet,
        // and SkillCastService.Cancel isn't surfaced — bolt-charge passthrough stays zeroed.
        ctx.Sc?.Start(src, StatusType.Spellfist, val1: skillLevel, val2: 0, val3: 0, 0, durationMs: 30_000, src);
    }
}
