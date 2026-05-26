using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// CG_MARIONETTE — Clown/Gypsy Marionette Control. Port of
/// <c>rathena-fork/src/map/status.cpp:11376-11414</c>
/// (status_change_start arms for SC_MARIONETTE + SC_MARIONETTE2).
///
/// <para>Pairs caster (SC_MARIONETTE) with target (SC_MARIONETTE2),
/// or unpairs if already linked. The caster <b>loses</b> half of each
/// of their base stats (str/agi/vit/int/dex/luk ÷ 2), and the target
/// gains those exact deltas, capped at <c>battle_config.max_parameter</c>
/// (default 99) so the target never exceeds the param cap.</para>
///
/// <para>Stat deltas are packed into Val3 (str|agi|vit) and Val4
/// (int|dex|luk) at byte granularity, matching the rAthena wire format
/// (used by status_calc_str / _agi / _vit / _int / _dex / _luk at
/// status.cpp:6782, 6853, 6917, etc).</para>
/// </summary>
public sealed class MarionetteControl : SkillImpl
{
    /// <summary>rAthena <c>battle_config.max_parameter</c> default (99).
    /// Target-side delta is capped to (max_parameter − target.stat) so
    /// transferred stats never push the target over the cap.</summary>
    public const int DefaultMaxParameter = 99;

    public MarionetteControl() : base(SkillIds.CG_MARIONETTE) { }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        if (src is not PlayerEntity sd || target is not PlayerEntity td)
        {
            if (src is PlayerEntity p)
                ctx.Client?.BroadcastSkillFail(p, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }

        // rAthena status.cpp:11378-11388 — caster.stat/2 packed into Val3/Val4 byte triples.
        // Val3 = (str<<16) | (agi<<8) | vit  ; Val4 = (int<<16) | (dex<<8) | luk.
        int srcStrHalf = Math.Clamp(sd.Stats.Str / 2, 0, 0xFF);
        int srcAgiHalf = Math.Clamp(sd.Stats.Agi / 2, 0, 0xFF);
        int srcVitHalf = Math.Clamp(sd.Stats.Vit / 2, 0, 0xFF);
        int srcIntHalf = Math.Clamp(sd.Stats.IntStat / 2, 0, 0xFF);
        int srcDexHalf = Math.Clamp(sd.Stats.Dex / 2, 0, 0xFF);
        int srcLukHalf = Math.Clamp(sd.Stats.Luk / 2, 0, 0xFF);

        int casterVal3 = (srcStrHalf << 16) | (srcAgiHalf << 8) | srcVitHalf;
        int casterVal4 = (srcIntHalf << 16) | (srcDexHalf << 8) | srcLukHalf;

        // rAthena status.cpp:11404-11412 — target deltas = min(source/2, max_param - target.stat).
        int maxParam = DefaultMaxParameter;
        int tgtStrDelta = Math.Clamp(Math.Min(srcStrHalf, maxParam - td.Stats.Str), 0, 0xFF);
        int tgtAgiDelta = Math.Clamp(Math.Min(srcAgiHalf, maxParam - td.Stats.Agi), 0, 0xFF);
        int tgtVitDelta = Math.Clamp(Math.Min(srcVitHalf, maxParam - td.Stats.Vit), 0, 0xFF);
        int tgtIntDelta = Math.Clamp(Math.Min(srcIntHalf, maxParam - td.Stats.IntStat), 0, 0xFF);
        int tgtDexDelta = Math.Clamp(Math.Min(srcDexHalf, maxParam - td.Stats.Dex), 0, 0xFF);
        int tgtLukDelta = Math.Clamp(Math.Min(srcLukHalf, maxParam - td.Stats.Luk), 0, 0xFF);

        int targetVal3 = (tgtStrDelta << 16) | (tgtAgiDelta << 8) | tgtVitDelta;
        int targetVal4 = (tgtIntDelta << 16) | (tgtDexDelta << 8) | tgtLukDelta;

        const int duration = 60_000;
        ctx.Sc?.Start(src, StatusType.Marionette,
            val1: (int)target.Id, val2: 0, val3: casterVal3, val4: casterVal4,
            durationMs: duration, source: src);
        ctx.Sc?.Start(target, StatusType.Marionette2,
            val1: (int)src.Id, val2: 0, val3: targetVal3, val4: targetVal4,
            durationMs: duration, source: src);

        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
    }
}
