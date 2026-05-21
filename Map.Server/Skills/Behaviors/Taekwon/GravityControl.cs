using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SJ_GRAVITYCONTROL — Gravity Control. Applies SC_GRAVITYCONTROL with computed fall damage (val2). Damage projection TODO.</summary>
public sealed class GravityControl : StatusSkillImpl { public GravityControl() : base(SkillIds.SJ_GRAVITYCONTROL) { } }
