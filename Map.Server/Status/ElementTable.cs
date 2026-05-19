namespace Map.Server.Status;

/// <summary>
/// Renewal elemental attribute multiplier table from
/// <c>rathena/db/re/attr_fix.yml</c>. Index = <c>[defLevel-1, atkElement,
/// defElement]</c>; values are %% multipliers (100 = 1.0×). Identical to
/// the in-memory <c>elemental_attribute_db</c> rAthena builds in
/// <c>db_main.cpp</c>.
///
/// Renewal only — pre-renewal is permanently out of scope (CLAUDE.md).
/// </summary>
public static class ElementTable
{
    // [Lv-1, atk, def]; only the 10 "real" elements (Neutral..Undead).
    // Holes (e.g. attackers without rows in the YAML) default to 100%.
    public const int Levels = 4;
    public const int Elements = 10; // Neutral..Undead

    private static readonly int[,,] Table = BuildTable();

    /// <summary>
    /// Look up the attacker × defender × defense-level multiplier (in %).
    /// Mirrors rAthena <c>elemental_attribute_db.getAttribute</c>
    /// (status.cpp: <c>battle_attr_fix</c> uses this verbatim).
    /// </summary>
    public static int GetRate(BattleElement atk, BattleElement def, int defLevel)
    {
        if (atk < 0 || (int)atk >= Elements) return 100;
        if (def < 0 || (int)def >= Elements) return 100;
        var lvIdx = Math.Clamp(defLevel - 1, 0, Levels - 1);
        return Table[lvIdx, (int)atk, (int)def];
    }

    private static int[,,] BuildTable()
    {
        var t = new int[Levels, Elements, Elements];
        // Default 100% everywhere — rAthena ATTRIBUTE_DB defaults missing
        // entries to 100, so any hole we leave matches their behavior.
        for (var l = 0; l < Levels; l++)
            for (var a = 0; a < Elements; a++)
                for (var d = 0; d < Elements; d++)
                    t[l, a, d] = 100;

        // --- Level 1 ---
        Set(t, 0, BattleElement.Neutral, BattleElement.Ghost, 90);
        Set(t, 0, BattleElement.Water, BattleElement.Water, 25);
        Set(t, 0, BattleElement.Water, BattleElement.Fire, 150);
        Set(t, 0, BattleElement.Water, BattleElement.Wind, 90);
        Set(t, 0, BattleElement.Water, BattleElement.Poison, 150);
        Set(t, 0, BattleElement.Earth, BattleElement.Earth, 25);
        Set(t, 0, BattleElement.Earth, BattleElement.Fire, 90);
        Set(t, 0, BattleElement.Earth, BattleElement.Wind, 150);
        Set(t, 0, BattleElement.Earth, BattleElement.Poison, 150);
        Set(t, 0, BattleElement.Fire, BattleElement.Water, 90);
        Set(t, 0, BattleElement.Fire, BattleElement.Earth, 150);
        Set(t, 0, BattleElement.Fire, BattleElement.Fire, 25);
        Set(t, 0, BattleElement.Fire, BattleElement.Poison, 150);
        Set(t, 0, BattleElement.Fire, BattleElement.Undead, 125);
        Set(t, 0, BattleElement.Wind, BattleElement.Water, 150);
        Set(t, 0, BattleElement.Wind, BattleElement.Earth, 90);
        Set(t, 0, BattleElement.Wind, BattleElement.Wind, 25);
        Set(t, 0, BattleElement.Wind, BattleElement.Poison, 150);
        Set(t, 0, BattleElement.Poison, BattleElement.Water, 150);
        Set(t, 0, BattleElement.Poison, BattleElement.Earth, 150);
        Set(t, 0, BattleElement.Poison, BattleElement.Fire, 150);
        Set(t, 0, BattleElement.Poison, BattleElement.Wind, 150);
        Set(t, 0, BattleElement.Poison, BattleElement.Poison, 0);
        Set(t, 0, BattleElement.Poison, BattleElement.Holy, 75);
        Set(t, 0, BattleElement.Poison, BattleElement.Dark, 75);
        Set(t, 0, BattleElement.Poison, BattleElement.Ghost, 75);
        Set(t, 0, BattleElement.Poison, BattleElement.Undead, 75);
        Set(t, 0, BattleElement.Holy, BattleElement.Poison, 75);
        Set(t, 0, BattleElement.Holy, BattleElement.Holy, 0);
        Set(t, 0, BattleElement.Holy, BattleElement.Dark, 125);
        Set(t, 0, BattleElement.Holy, BattleElement.Undead, 125);
        Set(t, 0, BattleElement.Dark, BattleElement.Poison, 75);
        Set(t, 0, BattleElement.Dark, BattleElement.Holy, 125);
        Set(t, 0, BattleElement.Dark, BattleElement.Dark, 0);
        Set(t, 0, BattleElement.Dark, BattleElement.Undead, 0);
        Set(t, 0, BattleElement.Ghost, BattleElement.Neutral, 90);
        Set(t, 0, BattleElement.Ghost, BattleElement.Poison, 75);
        Set(t, 0, BattleElement.Ghost, BattleElement.Holy, 90);
        Set(t, 0, BattleElement.Ghost, BattleElement.Dark, 90);
        Set(t, 0, BattleElement.Ghost, BattleElement.Ghost, 125);
        Set(t, 0, BattleElement.Undead, BattleElement.Fire, 90);
        Set(t, 0, BattleElement.Undead, BattleElement.Poison, 75);
        Set(t, 0, BattleElement.Undead, BattleElement.Holy, 125);
        Set(t, 0, BattleElement.Undead, BattleElement.Dark, 0);
        Set(t, 0, BattleElement.Undead, BattleElement.Undead, 0);

        // --- Level 2 ---
        Set(t, 1, BattleElement.Neutral, BattleElement.Ghost, 70);
        Set(t, 1, BattleElement.Water, BattleElement.Water, 0);
        Set(t, 1, BattleElement.Water, BattleElement.Fire, 175);
        Set(t, 1, BattleElement.Water, BattleElement.Wind, 80);
        Set(t, 1, BattleElement.Water, BattleElement.Poison, 150);
        Set(t, 1, BattleElement.Earth, BattleElement.Earth, 0);
        Set(t, 1, BattleElement.Earth, BattleElement.Fire, 80);
        Set(t, 1, BattleElement.Earth, BattleElement.Wind, 175);
        Set(t, 1, BattleElement.Earth, BattleElement.Poison, 150);
        Set(t, 1, BattleElement.Fire, BattleElement.Water, 80);
        Set(t, 1, BattleElement.Fire, BattleElement.Earth, 175);
        Set(t, 1, BattleElement.Fire, BattleElement.Fire, 0);
        Set(t, 1, BattleElement.Fire, BattleElement.Poison, 150);
        Set(t, 1, BattleElement.Fire, BattleElement.Undead, 150);
        Set(t, 1, BattleElement.Wind, BattleElement.Water, 175);
        Set(t, 1, BattleElement.Wind, BattleElement.Earth, 80);
        Set(t, 1, BattleElement.Wind, BattleElement.Wind, 0);
        Set(t, 1, BattleElement.Wind, BattleElement.Poison, 150);
        Set(t, 1, BattleElement.Poison, BattleElement.Water, 150);
        Set(t, 1, BattleElement.Poison, BattleElement.Earth, 150);
        Set(t, 1, BattleElement.Poison, BattleElement.Fire, 150);
        Set(t, 1, BattleElement.Poison, BattleElement.Wind, 150);
        Set(t, 1, BattleElement.Poison, BattleElement.Poison, 0);
        Set(t, 1, BattleElement.Poison, BattleElement.Holy, 75);
        Set(t, 1, BattleElement.Poison, BattleElement.Dark, 75);
        Set(t, 1, BattleElement.Poison, BattleElement.Ghost, 75);
        Set(t, 1, BattleElement.Poison, BattleElement.Undead, 50);
        Set(t, 1, BattleElement.Holy, BattleElement.Poison, 75);
        Set(t, 1, BattleElement.Holy, BattleElement.Holy, 0);
        Set(t, 1, BattleElement.Holy, BattleElement.Dark, 150);
        Set(t, 1, BattleElement.Holy, BattleElement.Undead, 150);
        Set(t, 1, BattleElement.Dark, BattleElement.Poison, 75);
        Set(t, 1, BattleElement.Dark, BattleElement.Holy, 150);
        Set(t, 1, BattleElement.Dark, BattleElement.Dark, 0);
        Set(t, 1, BattleElement.Dark, BattleElement.Undead, 0);
        Set(t, 1, BattleElement.Ghost, BattleElement.Neutral, 70);
        Set(t, 1, BattleElement.Ghost, BattleElement.Poison, 75);
        Set(t, 1, BattleElement.Ghost, BattleElement.Holy, 80);
        Set(t, 1, BattleElement.Ghost, BattleElement.Dark, 80);
        Set(t, 1, BattleElement.Ghost, BattleElement.Ghost, 150);
        Set(t, 1, BattleElement.Ghost, BattleElement.Undead, 125);
        Set(t, 1, BattleElement.Undead, BattleElement.Fire, 80);
        Set(t, 1, BattleElement.Undead, BattleElement.Poison, 50);
        Set(t, 1, BattleElement.Undead, BattleElement.Holy, 150);
        Set(t, 1, BattleElement.Undead, BattleElement.Dark, 0);
        Set(t, 1, BattleElement.Undead, BattleElement.Ghost, 125);
        Set(t, 1, BattleElement.Undead, BattleElement.Undead, 0);

        // --- Level 3 ---
        Set(t, 2, BattleElement.Neutral, BattleElement.Ghost, 50);
        Set(t, 2, BattleElement.Water, BattleElement.Water, 0);
        Set(t, 2, BattleElement.Water, BattleElement.Fire, 200);
        Set(t, 2, BattleElement.Water, BattleElement.Wind, 70);
        Set(t, 2, BattleElement.Water, BattleElement.Poison, 125);
        Set(t, 2, BattleElement.Earth, BattleElement.Earth, 0);
        Set(t, 2, BattleElement.Earth, BattleElement.Fire, 70);
        Set(t, 2, BattleElement.Earth, BattleElement.Wind, 200);
        Set(t, 2, BattleElement.Earth, BattleElement.Poison, 125);
        Set(t, 2, BattleElement.Fire, BattleElement.Water, 70);
        Set(t, 2, BattleElement.Fire, BattleElement.Earth, 200);
        Set(t, 2, BattleElement.Fire, BattleElement.Fire, 0);
        Set(t, 2, BattleElement.Fire, BattleElement.Poison, 125);
        Set(t, 2, BattleElement.Fire, BattleElement.Undead, 175);
        Set(t, 2, BattleElement.Wind, BattleElement.Water, 200);
        Set(t, 2, BattleElement.Wind, BattleElement.Earth, 70);
        Set(t, 2, BattleElement.Wind, BattleElement.Wind, 0);
        Set(t, 2, BattleElement.Wind, BattleElement.Poison, 125);
        Set(t, 2, BattleElement.Poison, BattleElement.Water, 125);
        Set(t, 2, BattleElement.Poison, BattleElement.Earth, 125);
        Set(t, 2, BattleElement.Poison, BattleElement.Fire, 125);
        Set(t, 2, BattleElement.Poison, BattleElement.Wind, 125);
        Set(t, 2, BattleElement.Poison, BattleElement.Poison, 0);
        Set(t, 2, BattleElement.Poison, BattleElement.Holy, 50);
        Set(t, 2, BattleElement.Poison, BattleElement.Dark, 50);
        Set(t, 2, BattleElement.Poison, BattleElement.Ghost, 50);
        Set(t, 2, BattleElement.Poison, BattleElement.Undead, 25);
        Set(t, 2, BattleElement.Holy, BattleElement.Poison, 50);
        Set(t, 2, BattleElement.Holy, BattleElement.Holy, 0);
        Set(t, 2, BattleElement.Holy, BattleElement.Dark, 175);
        Set(t, 2, BattleElement.Holy, BattleElement.Undead, 175);
        Set(t, 2, BattleElement.Dark, BattleElement.Poison, 50);
        Set(t, 2, BattleElement.Dark, BattleElement.Holy, 175);
        Set(t, 2, BattleElement.Dark, BattleElement.Dark, 0);
        Set(t, 2, BattleElement.Dark, BattleElement.Undead, 0);
        Set(t, 2, BattleElement.Ghost, BattleElement.Neutral, 50);
        Set(t, 2, BattleElement.Ghost, BattleElement.Poison, 50);
        Set(t, 2, BattleElement.Ghost, BattleElement.Holy, 70);
        Set(t, 2, BattleElement.Ghost, BattleElement.Dark, 70);
        Set(t, 2, BattleElement.Ghost, BattleElement.Ghost, 175);
        Set(t, 2, BattleElement.Ghost, BattleElement.Undead, 150);
        Set(t, 2, BattleElement.Undead, BattleElement.Fire, 70);
        Set(t, 2, BattleElement.Undead, BattleElement.Poison, 25);
        Set(t, 2, BattleElement.Undead, BattleElement.Holy, 175);
        Set(t, 2, BattleElement.Undead, BattleElement.Dark, 0);
        Set(t, 2, BattleElement.Undead, BattleElement.Ghost, 150);
        Set(t, 2, BattleElement.Undead, BattleElement.Undead, 0);

        // --- Level 4 ---
        Set(t, 3, BattleElement.Neutral, BattleElement.Ghost, 0);
        Set(t, 3, BattleElement.Water, BattleElement.Water, 0);
        Set(t, 3, BattleElement.Water, BattleElement.Fire, 200);
        Set(t, 3, BattleElement.Water, BattleElement.Wind, 60);
        Set(t, 3, BattleElement.Water, BattleElement.Poison, 125);
        Set(t, 3, BattleElement.Earth, BattleElement.Earth, 0);
        Set(t, 3, BattleElement.Earth, BattleElement.Fire, 60);
        Set(t, 3, BattleElement.Earth, BattleElement.Wind, 200);
        Set(t, 3, BattleElement.Earth, BattleElement.Poison, 125);
        Set(t, 3, BattleElement.Fire, BattleElement.Water, 60);
        Set(t, 3, BattleElement.Fire, BattleElement.Earth, 200);
        Set(t, 3, BattleElement.Fire, BattleElement.Fire, 0);
        Set(t, 3, BattleElement.Fire, BattleElement.Poison, 125);
        Set(t, 3, BattleElement.Fire, BattleElement.Undead, 200);
        Set(t, 3, BattleElement.Wind, BattleElement.Water, 200);
        Set(t, 3, BattleElement.Wind, BattleElement.Earth, 60);
        Set(t, 3, BattleElement.Wind, BattleElement.Wind, 0);
        Set(t, 3, BattleElement.Wind, BattleElement.Poison, 125);
        Set(t, 3, BattleElement.Poison, BattleElement.Water, 125);
        Set(t, 3, BattleElement.Poison, BattleElement.Earth, 125);
        Set(t, 3, BattleElement.Poison, BattleElement.Fire, 125);
        Set(t, 3, BattleElement.Poison, BattleElement.Wind, 125);
        Set(t, 3, BattleElement.Poison, BattleElement.Poison, 0);
        Set(t, 3, BattleElement.Poison, BattleElement.Holy, 50);
        Set(t, 3, BattleElement.Poison, BattleElement.Dark, 50);
        Set(t, 3, BattleElement.Poison, BattleElement.Ghost, 50);
        Set(t, 3, BattleElement.Poison, BattleElement.Undead, 0);
        Set(t, 3, BattleElement.Holy, BattleElement.Poison, 50);
        Set(t, 3, BattleElement.Holy, BattleElement.Holy, 0);
        Set(t, 3, BattleElement.Holy, BattleElement.Dark, 200);
        Set(t, 3, BattleElement.Holy, BattleElement.Undead, 200);
        Set(t, 3, BattleElement.Dark, BattleElement.Poison, 50);
        Set(t, 3, BattleElement.Dark, BattleElement.Holy, 200);
        Set(t, 3, BattleElement.Dark, BattleElement.Dark, 0);
        Set(t, 3, BattleElement.Dark, BattleElement.Undead, 0);
        Set(t, 3, BattleElement.Ghost, BattleElement.Neutral, 0);
        Set(t, 3, BattleElement.Ghost, BattleElement.Poison, 50);
        Set(t, 3, BattleElement.Ghost, BattleElement.Holy, 60);
        Set(t, 3, BattleElement.Ghost, BattleElement.Dark, 60);
        Set(t, 3, BattleElement.Ghost, BattleElement.Ghost, 200);
        Set(t, 3, BattleElement.Ghost, BattleElement.Undead, 175);
        Set(t, 3, BattleElement.Undead, BattleElement.Fire, 60);
        Set(t, 3, BattleElement.Undead, BattleElement.Poison, 0);
        Set(t, 3, BattleElement.Undead, BattleElement.Holy, 200);
        Set(t, 3, BattleElement.Undead, BattleElement.Dark, 0);
        Set(t, 3, BattleElement.Undead, BattleElement.Ghost, 175);
        Set(t, 3, BattleElement.Undead, BattleElement.Undead, 0);

        return t;
    }

    private static void Set(int[,,] t, int lvIdx, BattleElement atk, BattleElement def, int rate)
        => t[lvIdx, (int)atk, (int)def] = rate;
}
