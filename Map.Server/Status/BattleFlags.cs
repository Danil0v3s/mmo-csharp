namespace Map.Server.Status;

/// <summary>
/// rAthena <c>enum e_battle_flag</c> (battle.hpp:44) — the BF_* damage-classification bitmask used by
/// flag-matched bonuses (<c>bonus3 bSubEle/bSubRace, …, bf</c>) and range/weapon gating.
/// </summary>
public static class BattleFlags
{
    public const int None = 0x0000;
    public const int Weapon = 0x0001;
    public const int Magic = 0x0002;
    public const int Misc = 0x0004;
    public const int Short = 0x0010;
    public const int Long = 0x0040;
    public const int Skill = 0x0100;
    public const int Normal = 0x0200;

    public const int WeaponMask = Weapon | Magic | Misc;
    public const int RangeMask = Short | Long;
    public const int SkillMask = Skill | Normal;

    /// <summary>
    /// rAthena <c>pc_bonus_subele</c>/<c>pc_bonus_subrace</c> flag defaulting (pc.cpp:3502): fill in
    /// the unspecified masks so an under-qualified <c>bonus3</c> flag matches the expected attacks.
    /// </summary>
    public static int Default(int flag)
    {
        if ((flag & RangeMask) == 0) flag |= Short | Long;
        if ((flag & WeaponMask) == 0) flag |= Weapon;
        if ((flag & SkillMask) == 0)
        {
            if ((flag & (Magic | Misc)) != 0) flag |= Skill;
            if ((flag & Weapon) != 0) flag |= Normal | Skill;
        }
        return flag;
    }

    /// <summary>
    /// rAthena cardfix flag-match test (battle.cpp:823): the bonus's flag AND the actual attack's flag
    /// must share a bit in EACH of the three masks.
    /// </summary>
    public static bool Matches(int bonusFlag, int attackFlag)
        => (bonusFlag & attackFlag & WeaponMask) != 0
        && (bonusFlag & attackFlag & RangeMask) != 0
        && (bonusFlag & attackFlag & SkillMask) != 0;

    /// <summary>Parse a script BF flag arg — an int or a <c>BF_X|BF_Y</c> token string.</summary>
    public static int FromToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return None;
        int flag = 0;
        foreach (var raw in token.Split('|'))
        {
            var p = raw.Trim();
            flag |= p.ToUpperInvariant() switch
            {
                "BF_WEAPON" => Weapon, "BF_MAGIC" => Magic, "BF_MISC" => Misc,
                "BF_SHORT" => Short, "BF_LONG" => Long, "BF_SKILL" => Skill, "BF_NORMAL" => Normal,
                _ => int.TryParse(p, out var n) ? n : 0,
            };
        }
        return flag;
    }
}
