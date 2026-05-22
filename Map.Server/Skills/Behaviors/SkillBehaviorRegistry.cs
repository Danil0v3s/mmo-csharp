namespace Map.Server.Skills.Behaviors;

/// <summary>
/// Strategy table — <see cref="SkillImpl.SkillId"/> →
/// <see cref="SkillImpl"/>. Collects all registered plugins from
/// DI and indexes them by their rAthena skill id.
///
/// Mirrors <c>rathena-fork/src/map/skills/skill_factory.cpp</c>:
/// each <see cref="SkillImpl"/> subclass is constructed once and
/// looked up by id at cast time.
/// </summary>
public sealed class SkillBehaviorRegistry
{
    private readonly Dictionary<ushort, SkillImpl> _byId = new();
    // SK.100-2 — shared no-op fallback used for skills that don't have
    // a hand-written SkillImpl yet. Has all the default virtuals from
    // SkillImpl (no damage / no SC / no stat mod), so call sites get a
    // valid object instead of a null check.
    private static readonly SkillImpl _noOp = new NoOpSkillImpl();

    public SkillBehaviorRegistry(IEnumerable<SkillImpl> impls)
    {
        foreach (var s in impls) _byId[s.SkillId] = s;
    }

    /// <summary>
    /// Strict lookup — returns null when no hand-written SkillImpl is
    /// registered. Use this in code paths that want to differentiate
    /// "skill has bespoke behavior" from "skill is on the default path"
    /// (e.g. SkillUnitService.Place skips unknown skills entirely).
    /// </summary>
    public SkillImpl? Get(ushort skillId) => _byId.GetValueOrDefault(skillId);

    /// <summary>
    /// SK.100-2 — bulk-backfilled lookup. Always returns a usable
    /// SkillImpl: hand-written one if registered, otherwise a shared
    /// no-op that runs every default SkillImpl virtual. Mirrors the
    /// "RegisterDefaultsForMissingTypes" pattern that closed the SC
    /// tail. Use this from dispatch sites that want to call
    /// CastendDamageId / CastendNoDamageId without null-checking.
    /// </summary>
    public SkillImpl GetOrDefault(ushort skillId)
        => _byId.GetValueOrDefault(skillId) ?? _noOp;

    public int Count => _byId.Count;

    /// <summary>
    /// True iff a hand-written SkillImpl is registered for
    /// <paramref name="skillId"/>. Useful for callers that want to log
    /// "running default behavior for skill X."
    /// </summary>
    public bool HasCustomImpl(ushort skillId) => _byId.ContainsKey(skillId);

    /// <summary>
    /// SK.100-2 — shared no-op SkillImpl. Uses ushort.MaxValue as
    /// the sentinel skill id (the SkillImplDiAuditTests audit rejects
    /// SkillId == 0 as a registry-collision risk; MaxValue is outside
    /// the rAthena skill_db range so it can't shadow a real skill).
    /// </summary>
    private sealed class NoOpSkillImpl : SkillImpl
    {
        public NoOpSkillImpl() : base(ushort.MaxValue) { }
        // All virtuals stay at SkillImpl defaults (pass-through / no-op).
    }
}
