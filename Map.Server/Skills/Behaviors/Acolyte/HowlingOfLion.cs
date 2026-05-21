using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Acolyte;

/// <summary>
/// SR_HOWLINGOFLION — Sura Howling Of Lion. Manual port of
/// <c>rathena-fork/src/map/skills/acolyte/howlingoflion.cpp</c>.
///
/// <para>Splash damage that strips Performer SC family (Swing Dance,
/// Symphony of Lovers, Moonlit Serenade, Rush Windmill, Echo Song,
/// Harmonize, Netherworld, Voice of Siren, Deep Sleep, Sircle of
/// Nature, Gloomy Day, Song of Mana, Dance With Wug, Saturday Night
/// Fever, Lerad's Dew, Melody of Sink, Beyond of Warcry, Unlimited
/// Humming Voice). Ratio: <c>-100 + 500 * lv</c>.</para>
/// </summary>
public sealed class HowlingOfLion : RecursiveDamageSplashSkillImpl
{
    public HowlingOfLion() : base(SkillIds.SR_HOWLINGOFLION) { }

    public override int CalculateSkillRatio(int baseRatio, Entity src, Entity target, ushort skillLevel)
    {
        return baseRatio + (-100 + 500 * skillLevel);
    }

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        CastendDamageId(src, target, skillLevel, ctx);
    }

    public override void ApplyAdditionalEffects(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // rAthena splash hook strips every Performer SC.
        var sc = ctx.Sc;
        if (sc == null) return;
        sc.End(target, StatusType.Swingdance);
        sc.End(target, StatusType.Symphonyoflover);
        sc.End(target, StatusType.Moonlitserenade);
        sc.End(target, StatusType.Rushwindmill);
        sc.End(target, StatusType.Echosong);
        sc.End(target, StatusType.Harmonize);
        sc.End(target, StatusType.Netherworld);
        sc.End(target, StatusType.Voiceofsiren);
        sc.End(target, StatusType.Deepsleep);
        sc.End(target, StatusType.Sircleofnature);
        sc.End(target, StatusType.Gloomyday);
        sc.End(target, StatusType.GloomydaySk);
        sc.End(target, StatusType.Songofmana);
        sc.End(target, StatusType.Dancewithwug);
        sc.End(target, StatusType.Saturdaynightfever);
        sc.End(target, StatusType.Leradsdew);
        sc.End(target, StatusType.Melodyofsink);
        sc.End(target, StatusType.Beyondofwarcry);
        sc.End(target, StatusType.Unlimitedhummingvoice);
    }
}
