using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Mage;

/// <summary>
/// SA_AUTOSPELL — Sage Hindsight (Autospell). Manual port of
/// <c>rathena-fork/src/map/skills/mage/hindsight.cpp</c>.
///
/// <para>The deterministic spell + max level pick from skill level:
/// lv 10 → Frost Diver, lv 8-9 → Fireball, lv 5-7 → Soul Strike,
/// lv 2-4 → random (Cold/Fire/Lightning Bolt), lv 1 → Napalm Beat.</para>
///
/// <para>INFRA-DEFERRED: rAthena's player branch opens an autospell-list
/// dialog (ZC_AUTOSPELLLIST) and waits for the user's pick before
/// starting SC_AUTOSPELL; our wire surface doesn't carry that packet
/// yet, so the player branch falls back to the same deterministic
/// pick the mob branch uses.</para>
/// </summary>
public sealed class Hindsight : SkillImpl
{
    private readonly Random _rng;

    public Hindsight() : base(SkillIds.SA_AUTOSPELL) => _rng = Random.Shared;

    public Hindsight(Random? rng = null) : base(SkillIds.SA_AUTOSPELL) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        // Both branches use the same deterministic spell-pick table; the
        // canonical rAthena player path opens an autospell-list dialog
        // (ZC_AUTOSPELLLIST) and waits for the user's pick before
        // starting SC_AUTOSPELL — our wire surface doesn't carry that
        // packet yet, so the player branch falls back to the same
        // deterministic pick the mob branch uses. The SC still applies
        // and procs on hit; the only missing piece is the player's
        // choice override.
        ushort spellId = 0;
        int maxLv = 1;
        if (skillLevel >= 10) { spellId = SkillIds.MG_FROSTDIVER; maxLv = skillLevel - 9; }
        else if (skillLevel >= 8) { spellId = SkillIds.MG_FIREBALL; maxLv = skillLevel - 7; }
        else if (skillLevel >= 5) { spellId = SkillIds.MG_SOULSTRIKE; maxLv = skillLevel - 4; }
        else if (skillLevel >= 2)
        {
            ushort[] bolts = { SkillIds.MG_COLDBOLT, SkillIds.MG_FIREBOLT, SkillIds.MG_LIGHTNINGBOLT };
            spellId = bolts[_rng.Next(3)];
            maxLv = skillLevel - 1;
        }
        else if (skillLevel > 0) { spellId = SkillIds.MG_NAPALMBEAT; maxLv = 3; }
        if (spellId == 0) return;
        ctx.Sc?.Start(src, StatusType.Autospell, val1: skillLevel, val2: spellId, val3: maxLv, 0, durationMs: 30_000, src);
    }
}
