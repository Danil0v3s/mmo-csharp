using Map.Server.Combat;
using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors;

/// <summary>
/// Base per-skill implementation class. Mirrors the
/// <c>rathena-fork/src/map/skills/skill_impl.hpp</c> hierarchy
/// 1:1 — each rAthena skill case in <c>skill.cpp</c> ports to a
/// dedicated <see cref="SkillImpl"/> subclass that overrides only
/// the hooks it specializes.
///
/// <para>This replaces the earlier <c>ISkillBehavior</c> single-method
/// interface, which collapsed every per-skill quirk into one
/// <c>Resolve</c> call. The per-hook layout below lets a plugin
/// modify just the ratio (e.g. <see cref="CalculateSkillRatio"/>),
/// just the hit rate (e.g. <see cref="ModifyHitRate"/>), or just the
/// post-hit proc (e.g. <see cref="ApplyAdditionalEffects"/>) — all
/// composing without re-implementing the surrounding pipeline.</para>
///
/// <para><b>Subclasses</b>:</para>
/// <list type="bullet">
///   <item><see cref="WeaponSkillImpl"/> — single-target weapon hit whose
///         per-skill ratio comes from <see cref="CalculateSkillRatio"/> (the
///         single ratio authority); <c>skill_db</c> <c>DamageRate</c> is the
///         no-plugin fallback only, never combined with the plugin ratio.</item>
///   <item><see cref="StatusSkillImpl"/> — no-damage cast that applies
///         (or ends) an SC on the target.</item>
///   <item><see cref="RecursiveDamageSplashSkillImpl"/> — splash hits
///         around a target / ground point with per-victim damage.</item>
/// </list>
///
/// <para><b>Per-skill conventions</b>:</para>
/// <list type="bullet">
///   <item>One file per skill under
///         <c>Map.Server/Skills/Behaviors/&lt;Class&gt;/</c>.</item>
///   <item>Class name matches the rAthena skill stem
///         (<c>Bash</c>, <c>MagnumBreak</c>, <c>FireBolt</c>).</item>
///   <item>Constructor passes the <see cref="SkillIds"/> constant
///         to <see cref="SkillImpl(ushort)"/>.</item>
/// </list>
/// </summary>
public abstract class SkillImpl
{
    public ushort SkillId { get; }

    protected SkillImpl(ushort skillId) { SkillId = skillId; }

    /// <summary>
    /// rAthena <c>skill_castend_nodamage_id</c> arm. Runs for casts
    /// that don't deal damage (buffs, status applications, summons).
    /// </summary>
    public virtual void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx) { }

    /// <summary>
    /// rAthena <c>skill_castend_damage_id</c> arm. Runs for casts that
    /// deal damage to a single target.
    /// </summary>
    public virtual void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx) { }

    /// <summary>
    /// rAthena <c>skill_castend_pos2</c> arm. Runs for ground-targeted
    /// casts (typing-letter skills, splash AoE on a cell).
    /// </summary>
    public virtual void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx) { }

    /// <summary>
    /// rAthena <c>battle_calc_attack_skill_ratio</c> switch hook.
    /// Plugins override to add to the base ratio (e.g. Bash +30 %/lv).
    /// Default = pass-through.
    /// </summary>
    public virtual int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
        => baseRatio;

    /// <summary>
    /// SC-aware variant of <see cref="CalculateSkillRatio"/>. Plugins
    /// that need to consult the caster's status changes (e.g.
    /// <c>SC_SPEAR_SCAR</c>, <c>SC_DRAGONIC_AURA</c>) override this
    /// instead. The default delegates to the simpler overload so legacy
    /// plugins keep working.
    /// </summary>
    public virtual int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => CalculateSkillRatio(baseRatio, src, target, skillLevel);

    /// <summary>
    /// Full rAthena <c>calculateSkillRatio</c> shape — <paramref name="miscflag"/>
    /// is the per-hit flag set by the caller (path-AoE secondary hit,
    /// ground-unit dispatch, etc.). Plugins that branch on
    /// <c>SKILL_ALTDMG_FLAG</c> (SS_FUUMAKOUCHIKU alt-dmg) or the
    /// SkillCallType bit (NPC_LEX_AETERNA passthrough) override this
    /// overload. Default delegates to the ctx-aware overload so plugins
    /// that don't care about miscflag keep working.
    /// </summary>
    public virtual int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
        => CalculateSkillRatio(baseRatio, src, target, skillLevel, ctx);

    /// <summary>
    /// rAthena <c>battle_calc_skill_constant_addition</c> (battle.cpp:6606).
    /// A FLAT additive (not a percent) applied <b>after</b> the skill ratio
    /// and before cardfix/defense — rAthena <c>ATK_ADD(... constant ...)</c>
    /// at battle.cpp:7711, right after <c>ATK_RATE(... skillratio ...)</c>.
    /// Default 0. Examples: MO_EXTREMITYFIST <c>250 + 150*lv</c>,
    /// PA_SHIELDCHAIN's shield-weight bonus. Plugins override to add it.
    /// </summary>
    public virtual long CalculateSkillConstantAddition(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => CalculateSkillConstantAddition(src, target, skillLevel);

    /// <summary>
    /// SKILL-05 — ctx-free constant-addition (no plugin reads <c>ctx</c> for the
    /// flat add today). The ctx overload forwards here, so both the
    /// ResolveSkill→<see cref="WeaponSkillImpl.CastendDamageId"/> path and the
    /// ctx-less <c>SkillAttackService</c>/resolver funnel get the same value.
    /// </summary>
    public virtual long CalculateSkillConstantAddition(Entity src, Entity target, ushort skillLevel)
        => 0;

    /// <summary>
    /// rAthena <c>RE_LVL_DMOD(val)</c> divisor for this skill (config/const.hpp:94).
    /// Renewal scales the skill ratio by <c>baseLevel / divisor</c> when the
    /// caster's base level &gt; 99. Most <c>battle_calc_attack_skill_ratio</c>
    /// arms use <c>RE_LVL_DMOD(100)</c> (the default here); a few use 120/150,
    /// and fixed-damage skills omit the macro entirely — those override this to
    /// return <c>0</c> (disable, mirroring rAthena's <c>val &gt; 0</c> guard).
    /// </summary>
    protected virtual int ReLvlDivisor => 100;

    /// <summary>
    /// Apply <c>RE_LVL_DMOD(divisor)</c> to a skill ratio: above base level 99,
    /// <c>ratio * baseLevel / divisor</c>; at/below 99 (or divisor 0) unchanged.
    /// rAthena config/const.hpp:94.
    /// </summary>
    protected static int ApplyReLvlDmod(int ratio, Entity src, int divisor)
        => (src.Level > 99 && divisor > 0) ? (int)((long)ratio * src.Level / divisor) : ratio;

    /// <summary>
    /// rAthena <c>SKILL_ALTDMG_FLAG</c> — set on the secondary path-AoE
    /// hit pass when a skill re-fires through <c>skill_attack_area</c>.
    /// Plugins consult this on the miscflag-aware ratio overload to
    /// add the per-skill alt-dmg ratio bump.
    /// </summary>
    public const int SKILL_ALTDMG_FLAG = 0x1;

    /// <summary>
    /// rAthena per-skill hit-rate modifier. Bash adds +5 % hit per lv,
    /// Frost Diver +5 %, etc.
    /// </summary>
    public virtual short ModifyHitRate(short hitRate, Entity src, Entity target, ushort skillLevel)
        => hitRate;

    /// <summary>
    /// rAthena <c>skill_additional_effect</c> — post-hit procs on the
    /// target (Bash stun, FrostDiver freeze, BS_HAMMERFALL stun, etc.).
    /// </summary>
    public virtual void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx) { }

    /// <summary>
    /// rAthena <c>skill_counter_additional_effect</c> — post-hit procs
    /// on the caster (gain SP from drain, gain combo, etc.).
    /// </summary>
    public virtual void ApplyCounterAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx) { }

    /// <summary>
    /// rAthena <c>modifyDamageData</c> — gives the plugin one last
    /// shot at the Damage struct (flag/div_/type tweaks) right after
    /// the damage pipeline initialized it. Default: pass-through.
    /// </summary>
    public virtual void ModifyDamageData(ref Map.Server.Combat.BattleDamage dmg, Entity src, Entity target, ushort skillLevel) { }
}

/// <summary>
/// Specialized base for single-target weapon attacks. Provides a
/// default <see cref="CastendDamageId"/> that runs the standard
/// melee pipeline: calc swing → apply ratio modifier → apply damage
/// → run additional effects.
///
/// Subclasses typically override only <see cref="CalculateSkillRatio"/>
/// and <see cref="ApplyAdditionalEffects"/>.
/// </summary>
public abstract class WeaponSkillImpl : SkillImpl
{
    protected WeaponSkillImpl(ushort skillId) : base(skillId) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => CastendDamageId(src, target, skillLevel, ctx, miscflag: 0);

    /// <summary>
    /// Miscflag-aware overload — used by splash / path-AoE dispatchers
    /// that need to fold the per-hit alt-dmg flag into the ratio calc
    /// (rAthena <c>SKILL_ALTDMG_FLAG</c>: SS_FUUMAKOUCHIKU secondary hit).
    /// </summary>
    public virtual void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx, int miscflag)
    {
        var swing = ctx.Battle.CalcWeaponAttack(src, target);
        // SKILL-05: the ratio→ReLvlDmod→constant computation is the SINGLE
        // entry point ComputeSkillDamage, shared with SkillAttackService /
        // WeaponSkillResolver so a plugin skill can never get two different
        // ratios depending on which dispatch path it takes.
        var dmg = ComputeSkillDamage(swing, src, target, skillLevel, ctx, miscflag);
        // COMBAT-17 — render the skill's hit count (rAthena skill_get_num /
        // skill_db `num`). The ratio above already produced the full
        // multi-hit total (rAthena's negative-div "single damage shown as N
        // hits"), so we only set the display div — no extra multiplication.
        ctx.Damage.ApplyDamage(target, dmg, src, hits: GetMultiHitCount(skillLevel));
        ApplyAdditionalEffects(src, target, skillLevel, ctx);
    }

    /// <summary>
    /// COMBAT-17 — the skill's displayed hit count (rAthena
    /// <c>skill_get_num</c> / skill_db <c>num</c>). Drives
    /// <c>ZC_NOTIFY_ACT3.div</c>. Default 1; multi-hit weapon skills override
    /// (Sonic Blow → 8). Return the absolute display count — rAthena stores
    /// the "single damage split into N" skills as a negative <c>num</c>, but
    /// the wire and HP application both use the magnitude (the ratio already
    /// carries the full total).
    /// </summary>
    public virtual int GetMultiHitCount(ushort skillLevel) => 1;

    /// <summary>
    /// SKILL-05 — the canonical per-skill weapon-damage formula, used by both
    /// <see cref="CastendDamageId"/> (plugin dispatch, with <paramref name="ctx"/>)
    /// and the ctx-less <c>SkillAttackService</c> / <c>WeaponSkillResolver</c>
    /// funnels. rAthena order (battle.cpp:7708-7711): <c>ATK_RATE(skillratio)</c>
    /// then <c>ATK_ADD(constant)</c>; the renewal <c>RE_LVL_DMOD</c> multiplies
    /// the ratio first (COMBAT-03). This is the ONLY skill-ratio authority for a
    /// plugin skill — <see cref="SkillDefinition.DamageRate"/> is the no-plugin
    /// fallback only. When <paramref name="ctx"/> is null (the funnel has no
    /// <see cref="SkillBehaviorContext"/>) the ctx-free ratio/constant overloads
    /// are used; ctx-reading ratio overrides are honored only on the plugin path
    /// (SKILL-17).
    /// </summary>
    public int ComputeSkillDamage(
        Map.Server.Combat.BattleDamage swing, Entity src, Entity target,
        ushort skillLevel, SkillBehaviorContext? ctx = null, int miscflag = 0)
    {
        var ratio = ctx != null
            ? CalculateSkillRatio(100, src, target, skillLevel, ctx, miscflag)
            : CalculateSkillRatio(100, src, target, skillLevel);
        ratio = ApplyReLvlDmod(ratio, src, ReLvlDivisor);
        var raw = swing.Total * ratio / 100
                  + CalculateSkillConstantAddition(src, target, skillLevel);
        return (int)Math.Clamp(raw, 0, int.MaxValue);
    }
}

/// <summary>
/// Specialized base for buff / debuff / SC-applying skills.
/// Subclasses override <see cref="CastendNoDamageId"/> to attach the
/// SC; the <see cref="EndIfRunning"/> flag toggles the
/// recast-cures-rather-than-refreshes behavior (Lex Divina, Hiding,
/// Cloaking).
/// </summary>
public abstract class StatusSkillImpl : SkillImpl
{
    /// <summary>If true, a re-cast on a target already carrying the SC
    /// ends the SC instead of refreshing. Lex Divina + Hiding-style.</summary>
    protected bool EndIfRunning { get; }

    /// <summary>StatusType the skill applies (single-SC skills).
    /// Subclasses override to drive the toggle / cure path. Default
    /// = <see cref="StatusType.None"/> meaning "no automatic SC; the
    /// subclass owns the SC apply call".</summary>
    protected virtual StatusType TargetSc => StatusType.None;

    protected StatusSkillImpl(ushort skillId, bool endIfRunning = false) : base(skillId)
    {
        EndIfRunning = endIfRunning;
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // If the subclass declared its target SC + the recast-cures
        // semantic, honor it without per-subclass boilerplate.
        if (EndIfRunning && TargetSc != StatusType.None && ctx.Sc != null
            && ctx.Sc.Get(target, TargetSc) != null)
        {
            ctx.Sc.End(target, TargetSc);
            return;
        }
        // Otherwise the subclass owns the apply call via the SC apply
        // hook (it has the duration / Val1..Val4 math).
        ApplyAdditionalEffects(src, target, skillLevel, ctx);
    }
}

/// <summary>
/// Specialized base for splash-damage skills. Provides default
/// <see cref="CastendDamageId"/> + <see cref="CastendPos2"/> that
/// walk the splash victims and call <see cref="SplashDamage"/> on
/// each. Subclasses override <see cref="GetSplashSearchSize"/> to
/// set the radius and <see cref="SplashDamage"/> to compute the
/// per-victim damage.
/// </summary>
public abstract class RecursiveDamageSplashSkillImpl : SkillImpl
{
    protected RecursiveDamageSplashSkillImpl(ushort skillId) : base(skillId) { }

    public override void CastendDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
        => SplashAround(src, target.MapId, target.X, target.Y, skillLevel, ctx);

    public override void CastendPos2(Entity src, short x, short y, ushort skillLevel, SkillBehaviorContext ctx)
        => SplashAround(src, src.MapId, x, y, skillLevel, ctx);

    /// <summary>Cells to enumerate around the splash center. Default
    /// 2-cell radius (5×5).</summary>
    public virtual short GetSplashSearchSize(Entity src, ushort skillLevel) => 2;

    /// <summary>Which entity kinds are eligible victims. Default
    /// PvE-friendly: mobs + PCs. Source is skipped automatically.</summary>
    public virtual EntityType GetSplashTarget(Entity src)
        => EntityType.Mob | EntityType.Pc;

    /// <summary>Compute damage for one victim. Override per-skill.</summary>
    public virtual long SplashDamage(Entity src, Entity victim, ushort skillLevel, SkillBehaviorContext ctx)
        => 0;

    private void SplashAround(Entity src, uint mapId, short cx, short cy, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var radius = GetSplashSearchSize(src, skillLevel);
        var mask = GetSplashTarget(src);
        var victims = ctx.Entities.ForEachInRange(mapId, cx, cy, radius, mask);
        foreach (var v in victims)
        {
            if (v.Id == src.Id) continue;
            var dmg = SplashDamage(src, v, skillLevel, ctx);
            if (dmg > 0)
            {
                ctx.Damage.ApplyDamage(v, (int)Math.Clamp(dmg, 0, int.MaxValue), src);
            }
            ApplyAdditionalEffects(src, v, skillLevel, ctx);
        }
    }
}
