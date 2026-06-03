namespace Map.Server.Quest;

/// <summary>
/// The killed mob's matching attributes, passed to <see cref="IQuestService.UpdateMobObjective"/>.
/// Mirrors the fields rAthena's <c>quest_update_objective</c> reads off <c>mob_data</c> for the
/// any-mob objective filter: <c>mob_id</c>, level, race, size, and defense element.
/// </summary>
/// <param name="MobId">Mob class id (rAthena <c>md->mob_id</c>).</param>
/// <param name="Aegis">Mob aegis name (matched against a quest's specific <c>MobN</c> + the allow-list).</param>
/// <param name="Level">Mob level (<c>md->level</c>) — min/max level filter.</param>
/// <param name="Race">Mob race string (<c>md->status.race</c>), e.g. "DemiHuman".</param>
/// <param name="Size">Mob size string (<c>md->status.size</c>), e.g. "Medium".</param>
/// <param name="Element">Mob defense element string (<c>md->status.def_ele</c>), e.g. "Water".</param>
public readonly record struct QuestMobContext(
    int MobId, string Aegis, int Level, string Race, string Size, string Element);
