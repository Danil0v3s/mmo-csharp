using Map.Server.Entities;
using Map.Server.Inventory;
using Map.Server.Status;
using Microsoft.Extensions.Logging;

namespace Map.Server.Combat;

/// <summary>
/// Default <see cref="IBattleCardService"/>.
///
/// <para><b>CalcCardFix</b>: applies the attacker's percent damage
/// modifiers (race / element / size / class) via the aggregator on
/// <c>BattleStats</c>. Until the aggregator gains those fields the
/// method is a documented pass-through — it returns the input
/// damage unchanged. The canonical entry exists so the damage
/// pipeline doesn't need to be rewritten when the aggregator lands.
/// rAthena reference: battle.cpp:711.</para>
///
/// <para><b>AddMastery</b>: walks the attacker's
/// <see cref="PlayerEntity.LearnedSkills"/> for the rAthena
/// mastery skills and returns the additive bonus. Race / element
/// filters use the target's <c>BattleStats.Race</c> /
/// <c>DefenseElement</c>. rAthena reference: battle.cpp:2215.</para>
/// </summary>
public sealed class BattleCardService : IBattleCardService
{
    // rAthena skill ids (db/re/skill_db.yml). Hard-coded constants
    // here so the mastery lookup doesn't go through the skill_db on
    // every swing. Same approach we used in ShopService for
    // Discount/Overcharge.
    private const ushort AL_DEMONBANE     = 156;
    private const ushort HT_BEASTBANE     = 119;
    private const ushort BS_WEAPONRESEARCH = 122;
    private const ushort NC_RESEARCHFE    = 2502;
    private const ushort NC_MADOLICENCE   = 2501;
    private const ushort NV_BREAKTHROUGH  = 8000;
    private const ushort RA_RANGERMAIN    = 2351;
    // COMBAT-40 — weapon-type-gated mastery skills (battle_addmastery switch).
    private const ushort SM_SWORD          = 2;
    private const ushort SM_TWOHAND        = 3;
    private const ushort KN_SPEARMASTERY   = 55;
    private const ushort PR_MACEMASTERY    = 65;
    private const ushort AS_KATAR          = 134;
    private const ushort AM_AXEMASTERY     = 226;
    private const ushort MO_IRONHAND       = 259;
    private const ushort SA_ADVANCEDBOOK   = 274;
    private const ushort BA_MUSICALLESSON  = 315;
    private const ushort DC_DANCINGLESSON  = 323;
    private const ushort TK_RUN            = 411;
    private const ushort RK_DRAGONTRAINING = 2007;
    private const ushort NC_TRAININGAXE    = 2276;
    private const ushort GN_TRAINING_SWORD = 2474;

    private readonly ILogger<BattleCardService> _logger;

    // COMBAT-63 — lazy IStatusChangeService seam for battle_calc_cardfix_debuff. The SC
    // service depends on IDamageService → IBattleCalculator → IBattleCardService, so a
    // direct injection would form a construction cycle; the Lazy defers resolution to the
    // first combat SC read (same pattern as COMBAT-59 on BattleCalculator).
    private readonly Lazy<IStatusChangeService>? _scLazy;
    private IStatusChangeService? Sc => _scLazy?.Value;

    public BattleCardService(ILogger<BattleCardService> logger, Lazy<IStatusChangeService>? scLazy = null)
    {
        _logger = logger;
        _scLazy = scLazy;
    }

    public long CalcCardFix(BattleAttackType attackType, Entity src, Entity target, long damage, bool leftHand,
        BattleElement? attackElement = null)
    {
        if (damage == 0) return 0;

        // COMBAT-21 — rAthena battle_calc_cardfix (battle.cpp:711) is per-category
        // MULTIPLICATIVE: it accumulates `cardfix` (base 1000) by
        // `cardfix = cardfix * (100 ± fix) / 100` for each category, then applies
        // it once via APPLY_CARDFIX(damage, cardfix). The attacker (offensive)
        // and defender (defensive) sections accumulate + apply independently. This
        // replaces the earlier single additive `mult`, which drifted when multiple
        // categories stacked (e.g. +20% race + +20% size = ×1.44, not ×1.40).
        var ss = src.Stats;
        var ts = target.Stats;
        bool isMagic = attackType == BattleAttackType.Magic;

        int tRace = (int)ts.Race;
        int tEle = (int)ts.DefenseElement;
        int tSize = (int)ts.Size;
        int tClass = (ts.Mode & MobMode.Mvp) != 0
            ? (int)Inventory.BattleClassFlag.Boss : (int)Inventory.BattleClassFlag.Normal;

        // --- Attacker offensive cardfix (only a PC wears cards). Keyed on the
        //     TARGET's race / element / size / class. ---
        if (src is PlayerEntity pc && pc.EquipBonuses is { } ab)
        {
            int cardfix = 1000;
            // COMBAT-82 — arrow_addrace/arrow_addele apply only on a ranged (arrow) weapon swing
            // (battle.cpp:886-890), folded into the offensive race/ele categories.
            bool ranged = !isMagic && pc.Stats.AttackRange > 2;
            // COMBAT-21/63 — magic reads its own per-category arrays (magic_addrace/
            // magic_addele/magic_addsize/magic_addclass); weapon/misc use the weapon
            // addrace/addele/addsize/addclass. rAthena keeps the two sets distinct.
            int raceAdd = isMagic
                ? IdxAll(ab.MagicAddRace, tRace, (int)BattleRace.All)
                : IdxAll(ab.AddRace, tRace, (int)BattleRace.All)
                  + (ranged ? IdxAll(ab.ArrowAddRace, tRace, (int)BattleRace.All) : 0);
            // COMBAT-81 — race2 (RaceGroups) fold, summed across the target's groups. rAthena folds
            // it INTO the race multiply for magic (battle.cpp:795) and as its OWN category for weapon
            // (910/936).
            int race2Add = SumRace2(target, isMagic ? ab.MagicAddRace2 : ab.AddRace2);
            if (isMagic)
            {
                cardfix = cardfix * (100 + raceAdd + race2Add) / 100;
            }
            else
            {
                cardfix = cardfix * (100 + raceAdd) / 100;
                if (race2Add != 0) cardfix = cardfix * (100 + race2Add) / 100;
            }
            int eleAdd = IdxAll(isMagic ? ab.MagicAddEle : ab.AddEle, tEle, (int)BattleElement.All)
                         + (ranged ? IdxAll(ab.ArrowAddEle, tEle, (int)BattleElement.All) : 0);
            cardfix = cardfix * (100 + eleAdd) / 100;
            cardfix = cardfix * (100 + IdxAll(isMagic ? ab.MagicAddSize : ab.AddSize, tSize, (int)BattleSize.All)) / 100;
            cardfix = cardfix * (100 + IdxAll(isMagic ? ab.MagicAddClass : ab.AddClass, tClass, (int)Inventory.BattleClassFlag.All)) / 100;
            if (!isMagic)
            {
                // Short = melee (range ≤ 2), long = ranged.
                int rangeRate = pc.Stats.AttackRange > 2 ? ab.LongAtkRate : ab.ShortAtkRate;
                cardfix = cardfix * (100 + rangeRate) / 100;
            }
            else
            {
                // COMBAT-63 — battle_calc_cardfix_debuff (battle.cpp:667): the target's
                // element-debuff SCs raise the magic damage it takes. rh_ele here is the
                // resolved magic skill element (COMBAT-19).
                int debuff = MagicCardfixDebuff(target, attackElement ?? (BattleElement)ss.WeaponElement);
                if (debuff != 0) cardfix = cardfix * (100 + debuff) / 100;
            }
            damage = ApplyCardfix(damage, cardfix);
        }

        // --- Defender defensive cardfix (only a PC wears cards). Keyed on the
        //     ATTACKER's race / element / size / class; each category REDUCES.
        //     Works against mob attackers too (the old src-not-PC early-out
        //     skipped it). ---
        if (target is PlayerEntity tpc && tpc.EquipBonuses is { } db)
        {
            int cardfix = 1000;
            int aRace = (int)ss.Race;
            // COMBAT-19: magic/misc pass the resolved skill element; weapon (null)
            // uses the rh weapon element.
            int aEle = (int)(attackElement ?? (BattleElement)ss.WeaponElement);
            int aSize = (int)ss.Size;
            int aClass = (ss.Mode & MobMode.Mvp) != 0
                ? (int)Inventory.BattleClassFlag.Boss : (int)Inventory.BattleClassFlag.Normal;

            // COMBAT-82 — the actual attack's BF_* flag for the flag-matched lists. WeaponMask from the
            // lane (BattleAttackType == BF_WEAPON/MAGIC/MISC bits); RangeMask from the attacker's range;
            // SkillMask = both (the skill/normal discriminator isn't threaded — see COMBAT-99).
            int attackFlag = (int)attackType
                | (ss.AttackRange > 2 ? BattleFlags.Long : BattleFlags.Short)
                | BattleFlags.Skill | BattleFlags.Normal;

            // Element: card subele + flag-matched subele2; magic also adds magic_subdefele
            // (keyed on the ATTACKER's defense element, battle.cpp:829-830/836).
            int eleFix = IdxAll(db.SubEle, aEle, (int)BattleElement.All)
                         + FlagMatchedEle(db.SubEle2, aEle, attackFlag);
            if (isMagic) eleFix += IdxAll(db.MagicSubDefEle, (int)ss.DefenseElement, (int)BattleElement.All);
            cardfix = cardfix * (100 - eleFix) / 100;

            // Size: card subsize + magic_subsize (magic only, battle.cpp:839).
            int sizeFix = Idx(db.SubSize, aSize) + (isMagic ? IdxAll(db.MagicSubSize, aSize, (int)BattleSize.All) : 0);
            cardfix = cardfix * (100 - sizeFix) / 100;

            // Race: card subrace + flag-matched subrace3 (battle.cpp:846-855).
            int raceFix = IdxAll(db.SubRace, aRace, (int)BattleRace.All)
                          + FlagMatchedRace(db.SubRace3, aRace, attackFlag);
            cardfix = cardfix * (100 - raceFix) / 100;
            // COMBAT-81 — race2 reduction from the ATTACKER's race2 group(s) (battle.cpp:843).
            int sub2 = SumRace2(src, db.SubRace2);
            if (sub2 != 0) cardfix = cardfix * (100 - sub2) / 100;
            cardfix = cardfix * (100 - IdxAll(db.SubClass, aClass, (int)Inventory.BattleClassFlag.All)) / 100;
            damage = ApplyCardfix(damage, cardfix);
        }

        return Math.Max(1, damage);
    }

    /// <summary>Sum of the specific-index and the All-index entries (0 if out of range).</summary>
    private static int IdxAll(int[] arr, int idx, int allIdx) => Idx(arr, idx) + Idx(arr, allIdx);

    private static int Idx(int[] arr, int idx) => idx >= 0 && idx < arr.Length ? arr[idx] : 0;

    /// <summary>
    /// COMBAT-81 — sum a per-race2 bonus array over <paramref name="e"/>'s race2 set
    /// (rAthena <c>status_get_race2</c>: mobs only). Non-mob entities have no race2 → 0.
    /// </summary>
    /// <summary>
    /// COMBAT-82 — sum the flag-matched defensive element list (rAthena <c>subele2</c>, battle.cpp:820):
    /// entries whose element is the attack element (or ELE_ALL) AND whose BF_* flag matches the attack.
    /// </summary>
    private static int FlagMatchedEle(System.Collections.Generic.List<(int Ele, int Flag, int Rate)> list, int aEle, int attackFlag)
    {
        int sum = 0;
        for (int i = 0; i < list.Count; i++)
        {
            var it = list[i];
            if (it.Ele != (int)BattleElement.All && it.Ele != aEle) continue;
            if (!BattleFlags.Matches(it.Flag, attackFlag)) continue;
            sum += it.Rate;
        }
        return sum;
    }

    /// <summary>COMBAT-82 — flag-matched defensive race list (rAthena <c>subrace3</c>, battle.cpp:847).</summary>
    private static int FlagMatchedRace(System.Collections.Generic.List<(int Race, int Flag, int Rate)> list, int aRace, int attackFlag)
    {
        int sum = 0;
        for (int i = 0; i < list.Count; i++)
        {
            var it = list[i];
            if (it.Race != (int)BattleRace.All && it.Race != aRace) continue;
            if (!BattleFlags.Matches(it.Flag, attackFlag)) continue;
            sum += it.Rate;
        }
        return sum;
    }

    private static int SumRace2(Entity e, int[] arr)
    {
        if (e is not Entities.MobEntity m) return 0;
        var groups = m.Race2;
        if (groups.Count == 0) return 0;
        int sum = 0;
        for (int i = 0; i < groups.Count; i++) sum += Idx(arr, (int)groups[i]);
        return sum;
    }

    /// <summary>
    /// rAthena <c>APPLY_CARDFIX</c> (battle.cpp:748): apply a 1000-base
    /// accumulated <paramref name="cardfix"/> to <paramref name="damage"/>,
    /// rounding the reduction/increase down — <c>damage -= damage * (1000 -
    /// max(0, cardfix)) / 1000</c>. cardfix 1000 = no change; a category that
    /// pushes it ≤ 0 zeroes the damage.
    /// </summary>
    private static long ApplyCardfix(long damage, int cardfix)
        => damage - damage * (1000 - Math.Max(0, cardfix)) / 1000;

    /// <summary>
    /// rAthena <c>battle_calc_cardfix_debuff</c> (battle.cpp:667): the target's
    /// element-related debuff SCs add a flat % to the attacker's element-fix. SC_MAGIC_POISON
    /// is element-agnostic (+50); the Climax/Misty/Cloud SCs gate on the attack element.
    /// Returns 0 when no SC service is wired or no debuff is active.
    /// </summary>
    private int MagicCardfixDebuff(Entity target, BattleElement attackElement)
    {
        var sc = Sc;
        if (sc == null) return 0;

        int eleFix = 0;
        if (sc.Get(target, StatusType.MagicPoison) != null) eleFix += 50;
        switch (attackElement)
        {
            case BattleElement.Fire:
                if (sc.Get(target, StatusType.ClimaxBloom) != null) eleFix += 100;
                break;
            case BattleElement.Earth:
                if (sc.Get(target, StatusType.ClimaxEarth) != null) eleFix += 100;
                break;
            case BattleElement.Water:
                if (sc.Get(target, StatusType.Mistyfrost) != null) eleFix += 15;
                break;
            case BattleElement.Poison:
                var cp = sc.Get(target, StatusType.CloudPoison);
                if (cp != null) eleFix += 5 * cp.Val1;
                break;
        }
        return eleFix;
    }

    public long AddMastery(PlayerEntity attacker, Entity target, long damage, BattleAttackType type, int weaponType)
    {
        // rAthena: renewal returns only the bonus (caller does the
        // addition); pre-renewal mutates damage. We follow renewal —
        // the result is the additive bonus only.
        long bonus = 0;
        var ts = target.Stats;

        var demonBane = attacker.LearnedSkills.GetValueOrDefault(AL_DEMONBANE);
        if (demonBane > 0 && target is MobEntity
            && (IsUndead(ts) || ts.Race == BattleRace.Demon))
        {
            // rAthena: skill * (3 + (level+1)*0.05). Mobs only.
            bonus += (long)(demonBane * (3 + (attacker.Level + 1) * 0.05));
        }

        var rangerMain = attacker.LearnedSkills.GetValueOrDefault(RA_RANGERMAIN);
        if (rangerMain > 0
            && (ts.Race == BattleRace.Brute || ts.Race == BattleRace.Plant
                || ts.Race == BattleRace.Fish || ts.Race == BattleRace.PlayerDoram))
        {
            bonus += rangerMain * 5;
        }

        var researchFe = attacker.LearnedSkills.GetValueOrDefault(NC_RESEARCHFE);
        if (researchFe > 0
            && (ts.DefenseElement == BattleElement.Fire || ts.DefenseElement == BattleElement.Earth))
        {
            bonus += researchFe * 10;
        }

        // Madogear License is an unconditional bonus.
        bonus += 15 * attacker.LearnedSkills.GetValueOrDefault(NC_MADOLICENCE);

        var beastBane = attacker.LearnedSkills.GetValueOrDefault(HT_BEASTBANE);
        if (beastBane > 0
            && (ts.Race == BattleRace.Insect || ts.Race == BattleRace.Brute
                || ts.Race == BattleRace.PlayerDoram))
        {
            bonus += beastBane * 4;
        }

        // Weapon Research applies to all weapons (renewal).
        bonus += attacker.LearnedSkills.GetValueOrDefault(BS_WEAPONRESEARCH) * 2;

        var breakthrough = attacker.LearnedSkills.GetValueOrDefault(NV_BREAKTHROUGH);
        if (breakthrough > 0)
        {
            bonus += 15 * breakthrough + (breakthrough > 4 ? 25 : 0);
        }

        // Kagerou/Oboro Spirit Charm — bonus when full charm stack
        // matches target's defense element opposite (Fire vs Earth,
        // Water vs Fire, Land vs Wind, Wind vs Water).
        if (attacker.SpiritCharm >= 10)
        {
            var t = (CharmType)attacker.SpiritCharmType;
            var de = ts.DefenseElement;
            if ((t == CharmType.Fire && de == BattleElement.Earth)
                || (t == CharmType.Water && de == BattleElement.Fire)
                || (t == CharmType.Land && de == BattleElement.Wind)
                || (t == CharmType.Wind && de == BattleElement.Water))
            {
                bonus += attacker.Stats.Str * 2; // rAthena: STR scaled
            }
        }

        // COMBAT-40 — weapon-type-gated masteries (battle.cpp:2269-2335). The
        // caller passes the per-hand weapon type, so the dual-wield off-hand
        // resolves its mastery from the LEFT weapon (rAthena weapontype2), not
        // the main hand.
        bonus += WeaponMastery(attacker, weaponType);

        return bonus;
    }

    /// <summary>
    /// rAthena <c>battle_addmastery</c> weapon-type switch (battle.cpp:2269-2335):
    /// the learned weapon-mastery passive for <paramref name="weaponType"/>.
    /// </summary>
    private static long WeaponMastery(PlayerEntity sd, int weaponType)
    {
        long m = 0;
        int Lv(ushort id) => sd.LearnedSkills.GetValueOrDefault(id);
        switch (weaponType)
        {
            case WeaponTypeCodes.OneHandSword: // W_1HSWORD: +AxeMastery (RE), then sword/dagger
                m += Lv(AM_AXEMASTERY) * 3;
                goto case WeaponTypeCodes.Dagger;
            case WeaponTypeCodes.Dagger:       // W_DAGGER
                m += Lv(SM_SWORD) * 4;
                m += Lv(GN_TRAINING_SWORD) * 10;
                break;
            case WeaponTypeCodes.TwoHandSword: // W_2HSWORD
                m += Lv(SM_TWOHAND) * 4;
                break;
            case WeaponTypeCodes.OneHandSpear:
            case WeaponTypeCodes.TwoHandSpear: // W_1HSPEAR / W_2HSPEAR
            {
                var spear = Lv(KN_SPEARMASTERY);
                if (spear > 0)
                {
                    bool riding = (sd.Option & Map.Server.Status.PlayerOption.Riding) != 0
                        || sd.Option.HasDragon();
                    m += spear * (riding ? 5 : 4);
                    if (Lv(RK_DRAGONTRAINING) > 0) m += spear * 10;
                }
                break;
            }
            case WeaponTypeCodes.OneHandAxe:
            case WeaponTypeCodes.TwoHandAxe:   // W_1HAXE / W_2HAXE
                m += Lv(AM_AXEMASTERY) * 3;
                m += Lv(NC_TRAININGAXE) * 5;
                break;
            case WeaponTypeCodes.Mace:
            case WeaponTypeCodes.TwoHandMace:  // W_MACE / W_2HMACE
                m += Lv(PR_MACEMASTERY) * 3;
                m += Lv(NC_TRAININGAXE) * 4;
                break;
            case WeaponTypeCodes.Fist:         // W_FIST: +TK_RUN, then knuckle
                m += Lv(TK_RUN) * 10;
                goto case WeaponTypeCodes.Knuckle;
            case WeaponTypeCodes.Knuckle:      // W_KNUCKLE
                m += Lv(MO_IRONHAND) * 3;
                break;
            case WeaponTypeCodes.Musical:      // W_MUSICAL
                m += Lv(BA_MUSICALLESSON) * 3;
                break;
            case WeaponTypeCodes.Whip:         // W_WHIP
                m += Lv(DC_DANCINGLESSON) * 3;
                break;
            case WeaponTypeCodes.Book:         // W_BOOK
                m += Lv(SA_ADVANCEDBOOK) * 3;
                break;
            case WeaponTypeCodes.Katar:        // W_KATAR
                m += Lv(AS_KATAR) * 3;
                break;
        }
        return m;
    }

    private static bool IsUndead(Status.BattleStats s)
        => s.Race == BattleRace.Undead || s.DefenseElement == BattleElement.Undead;

    /// <summary>
    /// rAthena <c>battle_calc_chorusbonus</c> (battle.cpp:2847).
    /// Renewal path is hard <c>return 0</c> per the rAthena #ifdef
    /// guard — the chorus damage matrix is pre-renewal only. Our
    /// server is renewal, so this is structurally complete.
    /// Pre-renewal branch would count same-map party members with
    /// MAPID_THIRDMASK | MAPID_MINSTRELWANDERER and return 0 / (n-2) /
    /// 5 based on the rAthena thresholds.
    /// </summary>
    public int CalcChorusBonus(PlayerEntity attacker) => 0;
}

/// <summary>
/// Mirror of rAthena <c>e_charm_type</c> (status.hpp). Values
/// pinned to rAthena indices.
/// </summary>
public enum CharmType
{
    Water = 0,
    Land  = 1,
    Fire  = 2,
    Wind  = 3,
}
