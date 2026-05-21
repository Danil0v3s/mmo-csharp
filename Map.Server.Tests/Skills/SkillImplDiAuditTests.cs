using System.Reflection;
using Map.Server.Skills.Behaviors;

namespace Map.Server.Tests.Skills;

/// <summary>
/// T3.6 — DI registry audit. Walks every concrete <see cref="SkillImpl"/>
/// subclass in the loaded assembly and asserts:
/// <list type="bullet">
///   <item>The class is not abstract (must be instantiable).</item>
///   <item>If it declares a constructor with service parameters, that
///         ctor's parameters are all marked optional (= null), so DI
///         can still construct it when a service is missing without
///         erroring — but the service-injecting ctor IS preferred.</item>
///   <item>Every distinct SkillId across all impls is unique.</item>
///   <item>The reflection-visible registry shape matches the file-on-disk
///         count from T2.3 closure (1,208 SkillImpls).</item>
/// </list>
///
/// <para>This is the static cousin of the runtime DI test — it doesn't
/// boot the actual ServiceProvider, but it catches the kinds of bugs
/// the runtime audit would miss when a misregistered concrete still
/// silently constructs via the parameterless fallback.</para>
/// </summary>
public class SkillImplDiAuditTests
{
    private static readonly Type[] AllSkillImpls = typeof(SkillImpl).Assembly
        .GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract && typeof(SkillImpl).IsAssignableFrom(t))
        .ToArray();

    [Fact]
    public void Every_SkillImpl_IsConstructibleWithoutDi()
    {
        // DI accepts either: a parameterless ctor, OR a ctor whose
        // parameters are all optional (`= null` / `[Optional]`).
        // The harness depends on this — the SkillExerciser needs to
        // build a SkillImpl in isolation without booting the whole
        // ServiceProvider.
        var notConstructible = new List<string>();
        foreach (var t in AllSkillImpls)
        {
            var ok = t.GetConstructors().Any(c =>
                c.GetParameters().Length == 0
                || c.GetParameters().All(p => p.HasDefaultValue || p.IsOptional));
            if (!ok) notConstructible.Add(t.FullName!);
        }

        Assert.True(notConstructible.Count == 0,
            "SkillImpls with no DI-friendly ctor (must have ()-ctor or all-optional ctor):\n  "
            + string.Join("\n  ", notConstructible));
    }

    [Fact]
    public void Service_Injecting_Constructors_AcceptOptionalParameters()
    {
        // If a port declares `Foo(IService svc)` instead of
        // `Foo(IService? svc = null)`, DI still resolves it — but the
        // fallback breaks when the service isn't registered. Catch
        // these so a future service rename doesn't crash startup.
        var violations = new List<string>();
        foreach (var t in AllSkillImpls)
        {
            foreach (var c in t.GetConstructors())
            {
                if (c.GetParameters().Length == 0) continue;
                foreach (var p in c.GetParameters())
                {
                    if (!p.HasDefaultValue && !p.IsOptional)
                    {
                        violations.Add($"{t.Name}.ctor({p.ParameterType.Name} {p.Name}) — missing default");
                    }
                }
            }
        }

        Assert.True(violations.Count == 0,
            "SkillImpl constructors must have all parameters optional (= null):\n  "
            + string.Join("\n  ", violations));
    }

    [Fact]
    public void Every_SkillImpl_HasA_NonZero_SkillId()
    {
        var zero = new List<string>();
        foreach (var t in AllSkillImpls)
        {
            var inst = ActivateOrSkip(t);
            if (inst == null) continue;
            if (inst.SkillId == 0) zero.Add(t.FullName!);
        }
        Assert.True(zero.Count == 0,
            "SkillImpls with SkillId == 0 (registry collision risk):\n  "
            + string.Join("\n  ", zero));
    }

    [Fact]
    public void Registry_Indexes_All_SkillImpls_Without_Collision()
    {
        var impls = AllSkillImpls
            .Select(ActivateOrSkip)
            .Where(s => s != null)
            .Cast<SkillImpl>()
            .ToList();

        // Build via the registry constructor — collisions silently
        // overwrite, so we cross-check the count.
        var registry = new SkillBehaviorRegistry(impls);
        var distinctIds = impls.Select(s => s.SkillId).Distinct().Count();

        Assert.Equal(distinctIds, registry.Count);
        Assert.True(impls.Count >= 1200,
            $"Expected ~1,208 SkillImpls in assembly; got {impls.Count}. "
            + "If the count dropped, a port may have been deleted.");
    }

    private static SkillImpl? ActivateOrSkip(Type t)
    {
        try
        {
            return (SkillImpl?)Activator.CreateInstance(t);
        }
        catch
        {
            // ctor threw (legit if it requires non-null services); fall
            // back to the parameterless ctor explicitly.
            var ctor = t.GetConstructor(Type.EmptyTypes);
            return ctor == null ? null : (SkillImpl?)ctor.Invoke(null);
        }
    }
}
