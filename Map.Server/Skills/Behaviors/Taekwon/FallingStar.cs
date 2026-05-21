using Map.Server.Entities;

namespace Map.Server.Skills.Behaviors.Taekwon;

/// <summary>SJ_FALLINGSTAR — Falling Star. Status-only autocast; partner SJ_FALLINGSTAR_ATK handles +100*lv splash via its own stub.</summary>
public sealed class FallingStar : StatusSkillImpl { public FallingStar() : base(SkillIds.SJ_FALLINGSTAR) { } }
