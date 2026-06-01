using System.Collections.Generic;

namespace Map.Server.Status;

/// <summary>
/// rAthena <c>SCF_*</c> flag defaults per <see cref="StatusType"/>.
/// Mirrors the SC classification baked into status.cpp + status_yml
/// (the <c>Flags</c> block per SC). Used by
/// <see cref="StatusEffectRegistry.GetEffectiveFlags"/> so existing
/// <see cref="StatusEffectHandler"/> registrations that don't set
/// <see cref="StatusEffectHandler.Flags"/> explicitly inherit a
/// sensible default — buffs are flagged Buff + RemoveOnChangeMap +
/// RemoveOnLogout, debuffs flagged Debuff + RemoveOnRefresh, DoT
/// debuffs additionally flagged SpreadEffect where rAthena marks
/// them so.
///
/// This is the single source of truth for ScfFlag defaults; per-SC
/// overrides ride on the handler's own <c>Flags</c> field.
/// </summary>
public static class StatusFlagDefaults
{
    private static readonly Dictionary<StatusType, ScfFlag> _defaults = Build();

    public static ScfFlag For(StatusType type)
        => _defaults.GetValueOrDefault(type, ScfFlag.None);

    private static Dictionary<StatusType, ScfFlag> Build()
    {
        var d = new Dictionary<StatusType, ScfFlag>();

        // ===== CC debuffs — Refresh skill clears these =====
        var ccGate = ScfFlag.Debuff | ScfFlag.RemoveOnRefresh;
        d[StatusType.Stone] = ccGate;
        d[StatusType.Stonewait] = ccGate;
        d[StatusType.Freeze] = ccGate;
        d[StatusType.Stun] = ccGate;
        d[StatusType.Sleep] = ccGate | ScfFlag.RemoveOnDamaged;
        // SC-08: Curse/Silence/Confusion/Blind are also SCF_SPREADEFFECT.
        d[StatusType.Curse] = ccGate | ScfFlag.SpreadEffect;
        d[StatusType.Silence] = ccGate | ScfFlag.SpreadEffect;
        d[StatusType.Confusion] = ccGate | ScfFlag.SpreadEffect;
        d[StatusType.Blind] = ccGate | ScfFlag.SpreadEffect;
        d[StatusType.Provoke] = ScfFlag.Debuff | ScfFlag.RemoveOnRefresh;

        // ===== DoT debuffs =====
        // SC-08 — rAthena db/re/status.yml flags exactly 18 SCs SCF_SPREADEFFECT
        // (propagated by Shadow Chaser Deadly Infect via status_change_spread,
        // NOT on their own tick). Full set: Poison, Curse, Silence, Confusion,
        // Blind, Bleeding, Hallucination, Burning, Freezing, Toxin, Paralyse,
        // Venombleed, Magicmushroom, Deathhurt, Pyrexia, Oblivioncurse,
        // Leechesend, Bodypaint. (Influenza/MistyFrost are NOT flagged.)
        // DeadlyPoison does NOT spread.
        var spreadDot = ScfFlag.Debuff | ScfFlag.RemoveOnRefresh | ScfFlag.SpreadEffect;
        d[StatusType.Poison] = spreadDot;
        d[StatusType.DeadlyPoison] = ScfFlag.Debuff | ScfFlag.RemoveOnRefresh;
        d[StatusType.Bleeding] = spreadDot;
        d[StatusType.Burning] = spreadDot;
        // (Curse/Silence/Confusion/Blind get SpreadEffect in the CC-debuff block above.)
        d[StatusType.Hallucination] = spreadDot;
        d[StatusType.Freezing] = spreadDot;
        d[StatusType.Toxin] = spreadDot;
        d[StatusType.Paralyse] = spreadDot;
        d[StatusType.Venombleed] = spreadDot;
        d[StatusType.Magicmushroom] = spreadDot;
        d[StatusType.Deathhurt] = spreadDot;
        d[StatusType.Pyrexia] = spreadDot;
        d[StatusType.Oblivioncurse] = spreadDot;
        d[StatusType.Leechesend] = spreadDot;
        d[StatusType.Bodypaint] = spreadDot;

        // ===== Stat buffs (cleared by Dispell + drop on map change in some cases) =====
        // rAthena: most positive PC buffs flag Buff. Map-change clears
        // are SC-specific; common buffs (Blessing, IncreaseAgi, etc.)
        // survive map change in rAthena's default config.
        var pcBuff = ScfFlag.Buff | ScfFlag.RemoveOnLogout;
        d[StatusType.Blessing] = pcBuff;
        d[StatusType.IncreaseAgi] = pcBuff;
        d[StatusType.Angelus] = pcBuff;
        d[StatusType.Concentrate] = pcBuff;
        d[StatusType.Concentration] = pcBuff;
        d[StatusType.Adrenaline] = pcBuff;
        d[StatusType.Twohandquicken] = pcBuff;
        d[StatusType.Maximizepower] = pcBuff;
        d[StatusType.Overthrust] = pcBuff;
        d[StatusType.Berserk] = pcBuff;
        d[StatusType.Endure] = pcBuff;
        d[StatusType.Assumptio] = pcBuff;
        d[StatusType.Kyrie] = pcBuff;
        d[StatusType.Steelbody] = pcBuff;
        d[StatusType.Autoguard] = pcBuff;
        d[StatusType.Reflectshield] = pcBuff;
        d[StatusType.Sacrifice] = pcBuff;
        d[StatusType.Providence] = pcBuff;
        d[StatusType.Aspersio] = pcBuff;
        d[StatusType.Aeterna] = pcBuff;
        d[StatusType.Tensionrelax] = pcBuff;
        d[StatusType.Magnificat] = pcBuff;
        d[StatusType.Gloria] = pcBuff;
        d[StatusType.HealOverTime] = pcBuff;
        d[StatusType.Suffragium] = pcBuff;
        d[StatusType.Memorize] = pcBuff;
        d[StatusType.Poembragi] = pcBuff;
        d[StatusType.Impositio] = pcBuff;
        d[StatusType.Magicpower] = pcBuff;

        // ===== Stat debuffs (cleared by Dispell or Refresh) =====
        var pcDebuff = ScfFlag.Debuff | ScfFlag.RemoveOnRefresh;
        d[StatusType.DecreaseAgi] = pcDebuff;
        d[StatusType.Signumcrucis] = pcDebuff;
        d[StatusType.Slowcast] = pcDebuff;
        d[StatusType.Paralysis] = pcDebuff;
        d[StatusType.Striparmor] = pcDebuff;
        d[StatusType.Striphelm] = pcDebuff;
        d[StatusType.Stripshield] = pcDebuff;
        d[StatusType.Stripweapon] = pcDebuff;

        // ===== Stealth (cleared by attack) =====
        d[StatusType.Hiding] = ScfFlag.Buff | ScfFlag.RemoveOnDamaged;
        d[StatusType.Cloaking] = ScfFlag.Buff | ScfFlag.RemoveOnDamaged;

        // ===== Movement / utility =====
        d[StatusType.Windwalk] = pcBuff;
        d[StatusType.Cartboost] = pcBuff;

        // ===== Element-on-weapon =====
        d[StatusType.Fireweapon] = pcBuff;
        d[StatusType.Earthweapon] = pcBuff;
        d[StatusType.Windweapon] = pcBuff;
        d[StatusType.Waterweapon] = pcBuff;
        d[StatusType.Encpoison] = pcBuff;
        d[StatusType.Edp] = pcBuff;

        // ===== Job-specific markers =====
        d[StatusType.Akaitsuki] = pcBuff;
        d[StatusType.Saturdaynightfever] = ScfFlag.Debuff;
        d[StatusType.Adoramus] = pcDebuff;
        d[StatusType.DragonicAura] = pcBuff;
        d[StatusType.Laudaagnus] = pcBuff;
        d[StatusType.Laudaramus] = pcBuff;
        d[StatusType.Bitescar] = ScfFlag.Debuff;
        d[StatusType.Meltdown] = pcBuff;
        d[StatusType.Explosionspirits] = pcBuff;
        d[StatusType.Kaite] = pcBuff;
        d[StatusType.Deathbound] = pcBuff;
        d[StatusType.BasilicaCell] = ScfFlag.Permanent; // cell-based, never auto-cleared
        d[StatusType.Izayoi] = pcBuff;

        return d;
    }
}
