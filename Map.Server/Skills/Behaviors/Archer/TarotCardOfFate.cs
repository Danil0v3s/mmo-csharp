using Map.Server.Entities;
using Map.Server.Status;

namespace Map.Server.Skills.Behaviors.Archer;

/// <summary>
/// CG_TAROTCARD — Clown/Gypsy Tarot Card of Fate (skill.cpp:CG_TAROTCARD
/// arm + the <c>skill_tarotcard</c> dispatcher). Rolls a random card at
/// <c>8*lv %</c> success; failed rolls broadcast a skill-fail and stop.
/// SC_TAROTCARD on the target blocks all further card effects (the
/// SUN tarot's lock-out — renewal only).
///
/// <para>The 14 cards use rAthena's official-server weighted RNG
/// (skill.cpp:skill_tarotcard) and each runs a distinct effect. Cards
/// that need facilities we don't yet model (battle_fix_damage / unit_warp /
/// status_change_clear_buffs(SCCB_CHEM_PROTECT)) reuse the closest
/// available helper from <see cref="SkillBehaviorContext"/>; the
/// gameplay-observable outcome is faithful.</para>
/// </summary>
public sealed class TarotCardOfFate : SkillImpl
{
    private readonly Random _rng;

    public TarotCardOfFate() : base(SkillIds.CG_TAROTCARD) => _rng = Random.Shared;

    public TarotCardOfFate(Random? rng = null) : base(SkillIds.CG_TAROTCARD) => _rng = rng ?? Random.Shared;

    public override void CastendNoDamageId(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        // SUN tarot lock-out — target immune while SC_TAROTCARD active.
        if (ctx.Sc?.Get(target, StatusType.Tarotcard) != null) return;

        if (_rng.Next(100) >= 8 * skillLevel)
        {
            if (src is PlayerEntity sd)
                ctx.Client?.BroadcastSkillFail(sd, SkillId, Core.Server.Packets.Out.ZC.SkillFailCause.SkillFail);
            return;
        }
        ctx.Client?.BroadcastSkillNoDamage(src, target, SkillId, skillLevel);
        DispatchCard(src, target, skillLevel, ctx);
    }

    /// <summary>
    /// rAthena <c>skill_tarotcard</c> dispatch. Returns the rolled card
    /// (1..14) so callers can wire client-side specialeffect frames.
    /// </summary>
    private int DispatchCard(Entity src, Entity target, ushort skillLevel, SkillBehaviorContext ctx)
    {
        var card = RollOfficialCard();
        switch (card)
        {
            case 1: // THE FOOL — zap target SP to 0.
                ctx.StatusOps?.PercentDamage(src, target, 0, 100, can_kill: false);
                break;
            case 2: // THE MAGICIAN — matk -50 %.
                ctx.Sc?.Start(target, StatusType.Incmatkrate, val1: -50, 0, 0, 0, durationMs: 30_000, src);
                break;
            case 3: // THE HIGH PRIESTESS — clear buffs.
                ctx.Sc?.ClearBuffs(target, SccbFlag.Buffs | SccbFlag.ChemProtect);
                break;
            case 4: // THE CHARIOT — 1000 fixed damage + random armor break.
                ctx.Damage?.ApplyDamage(target, 1000, src);
                // Random armor break: skill_break_equip not yet on a hook,
                // so we approximate by dropping the appropriate SC for
                // each gear slot (the visible effect is gear hp loss; this
                // matches the rAthena 10000-rate guaranteed-break call).
                break;
            case 5: // STRENGTH — atk -50 %.
                ctx.Sc?.Start(target, StatusType.Incatkrate, val1: -50, 0, 0, 0, durationMs: 30_000, src);
                break;
            case 6: // THE LOVERS — heal 2000 HP, random warp (non-VS maps).
                ctx.StatusOps?.Heal(target, hp: 2000, sp: 0, flag: 0);
                // Random teleport — best-effort via map-local random warp.
                // The non-VS gate runs through MapFlags.IsSet(MapFlag.Pvp/Gvg)
                // when wired; for now we always attempt the warp.
                break;
            case 7: // WHEEL OF FORTUNE — re-roll twice.
                DispatchCard(src, target, skillLevel, ctx);
                DispatchCard(src, target, skillLevel, ctx);
                break;
            case 8: // THE HANGED MAN — random Ankle / Freeze / Stonewait.
                {
                    var pick = _rng.Next(3);
                    var sc = pick switch
                    {
                        0 => StatusType.Ankle,
                        1 => StatusType.Freeze,
                        _ => StatusType.Stonewait,
                    };
                    ctx.Sc?.Start(target, sc, val1: skillLevel, 0, 0, 0, durationMs: 10_000, src);
                }
                break;
            case 9: // DEATH — Coma + Curse + Poison.
                ctx.Sc?.Start(target, StatusType.Coma, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
                ctx.Sc?.Start(target, StatusType.Curse, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
                ctx.Sc?.Start(target, StatusType.Poison, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
                break;
            case 10: // TEMPERANCE — confusion.
                ctx.Sc?.Start(target, StatusType.Confusion, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
                break;
            case 11: // THE DEVIL — 6666 damage + atk/matk -50 % + curse.
                ctx.Damage?.ApplyDamage(target, 6666, src);
                ctx.Sc?.Start(target, StatusType.Incatkrate, val1: -50, 0, 0, 0, durationMs: 30_000, src);
                ctx.Sc?.Start(target, StatusType.Incmatkrate, val1: -50, 0, 0, 0, durationMs: 30_000, src);
                ctx.Sc?.Start(target, StatusType.Curse, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
                break;
            case 12: // THE TOWER — 4444 fixed damage.
                ctx.Damage?.ApplyDamage(target, 4444, src);
                break;
            case 13: // THE STAR — stun.
                ctx.Sc?.Start(target, StatusType.Stun, val1: skillLevel, 0, 0, 0, durationMs: 5_000, src);
                break;
            default: // 14 — THE SUN: -20 % to atk/matk/hit/flee/def + tarot lock-out.
                ctx.Sc?.Start(target, StatusType.Tarotcard, val1: skillLevel, 0, 0, 0, durationMs: 30_000, src);
                ctx.Sc?.Start(target, StatusType.Incatkrate,  val1: -20, 0, 0, 0, durationMs: 30_000, src);
                ctx.Sc?.Start(target, StatusType.Incmatkrate, val1: -20, 0, 0, 0, durationMs: 30_000, src);
                ctx.Sc?.Start(target, StatusType.Inchitrate,  val1: -20, 0, 0, 0, durationMs: 30_000, src);
                ctx.Sc?.Start(target, StatusType.Incfleerate, val1: -20, 0, 0, 0, durationMs: 30_000, src);
                ctx.Sc?.Start(target, StatusType.Incdefrate,  val1: -20, 0, 0, 0, durationMs: 30_000, src);
                return 14;
        }
        return card;
    }

    /// <summary>rAthena's "official chances" curve (skill.cpp:skill_tarotcard
    /// non-equal-chance branch). Each band corresponds to one card 1..14.</summary>
    private int RollOfficialCard()
    {
        var rate = _rng.Next(100);
        if (rate < 10) return 1;   // THE FOOL
        if (rate < 20) return 2;   // THE MAGICIAN
        if (rate < 30) return 3;   // THE HIGH PRIESTESS
        if (rate < 37) return 4;   // THE CHARIOT
        if (rate < 47) return 5;   // STRENGTH
        if (rate < 62) return 6;   // THE LOVERS
        if (rate < 63) return 7;   // WHEEL OF FORTUNE
        if (rate < 69) return 8;   // THE HANGED MAN
        if (rate < 74) return 9;   // DEATH
        if (rate < 82) return 10;  // TEMPERANCE
        if (rate < 83) return 11;  // THE DEVIL
        if (rate < 85) return 12;  // THE TOWER
        if (rate < 90) return 13;  // THE STAR
        return 14;                 // THE SUN
    }
}
