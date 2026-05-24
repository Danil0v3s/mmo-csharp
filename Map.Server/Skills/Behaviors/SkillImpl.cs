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
///   <item><see cref="WeaponSkillImpl"/> — single-target weapon hit
///         with skill_db DamageRate + per-skill ratio bump.</item>
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
    {
        var swing = ctx.Battle.CalcWeaponAttack(src, target);
        // Route through the ctx-aware overload so plugins that need
        // SC reads (DK_DRAGONIC_BREATH, LG_CANNONSPEAR's SC_SPEAR_SCAR,
        // SS_REIKETSUHOU's SC_WATER_CHARM_POWER) can hook them. The
        // default impl falls back to the simpler signature so legacy
        // plugins keep their existing behavior.
        var ratio = CalculateSkillRatio(100, src, target, skillLevel, ctx);
        var dmg = (int)Math.Clamp(swing.Total * ratio / 100, 0, int.MaxValue);
        ctx.Damage.ApplyDamage(target, dmg, src);
        ApplyAdditionalEffects(src, target, skillLevel, ctx);
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
