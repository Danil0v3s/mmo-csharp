namespace Core.Database.Entities;

// Static catalog entities — one row per rAthena _db entry. Bundled
// here to keep boilerplate dense; the EF configurations + repos +
// DbSets reference them by short name. These cover the second wave
// of Tier 1 in PARITY-CLOSURE-ROADMAP.md.
//
// For "payload" tables (nested YAML structures), the entity carries
// a primary key column + a `payload_json` text column. The runtime
// service deserializes the JSON into a typed record on Reload().

// ---- flat-shape tables ----

public class CastleDbEntity
{
    public uint CastleId { get; set; }
    public string? MapName { get; set; }
    public string? Name { get; set; }
    public string? NpcName { get; set; }
    public string? CastleType { get; set; }
    public int? ClientId { get; set; }
    public bool? WarpEnabled { get; set; }
    public int? WarpX { get; set; }
    public int? WarpY { get; set; }
}

public class StatPointEntity
{
    public int Level { get; set; }
    public int Points { get; set; }
    public int TraitPoints { get; set; }
}

public class ExpHomunEntity { public int Level { get; set; } public long Exp { get; set; } }
public class ExpGuildEntity { public int Level { get; set; } public long Exp { get; set; } }

public class SizeFixEntity
{
    public string Weapon { get; set; } = string.Empty;
    public int Small { get; set; } = 100;
    public int Medium { get; set; } = 100;
    public int Large { get; set; } = 100;
}

public class ReputationEntity
{
    public int ReputationId { get; set; }
    public string? Name { get; set; }
    public string? Variable { get; set; }
    public int Minimum { get; set; }
    public int Maximum { get; set; }
}

public class CreateArrowDbEntity
{
    public string SourceItem { get; set; } = string.Empty;
    public string MakeJson { get; set; } = "[]";
}

public class ItemRandomOptDbEntity
{
    public int OptionId { get; set; }
    public string? OptionName { get; set; }
    public string? Script { get; set; }
}

public class CashShopDbEntity
{
    public string Tab { get; set; } = string.Empty;
    public string ItemsJson { get; set; } = "[]";
}

public class CaptchaDbEntity
{
    public uint CaptchaId { get; set; }
    public string? Image { get; set; }
    public string? Answer { get; set; }
}

// ---- payload-shape tables (string key + JSON payload) ----

public abstract class PayloadStringKeyEntity { public string Key { get; set; } = string.Empty; public string PayloadJson { get; set; } = "{}"; }
public abstract class PayloadIntKeyEntity { public int Key { get; set; } public string PayloadJson { get; set; } = "{}"; }
public abstract class PayloadRowIndexEntity { public int RowId { get; set; } public string PayloadJson { get; set; } = "{}"; }

public class ElementalDbEntity : PayloadIntKeyEntity { } // ele_id
public class BattlegroundDbEntity : PayloadIntKeyEntity { } // bg_id
// SkillTreeEntity / GuildSkillTreeEntity removed in DB-8c — see
// SkillTreeDbEntities.cs (typed parent + Inherit + entry + requirement).
// MobSummonEntity removed in DB-8b — see MobSummonDbEntity + MobSummonEntryDbEntity.
public class ItemRandomOptGroupEntity : PayloadIntKeyEntity { } // group_id
// AttrFixEntity / LevelPenaltyEntity removed in DB-8a — replaced by
// typed AttrFixDbEntity + LevelPenaltyDbEntity / LevelPenaltyDifferenceDbEntity.
// JobStatsEntity / JobExpEntity / JobBasePointsEntity removed in DB-8d —
// see JobCatalogDbEntities.cs (JobInfoDbEntity + JobBonusStatsDbEntity +
// JobExpDbEntity + JobMaxLevelDbEntity + JobBasePointsDbEntity).
public class StatusYmlEntity : PayloadStringKeyEntity { } // status_name
// ItemCombosEntity removed in DB-8b — see ItemComboDbEntity + ItemComboMemberDbEntity.
// ItemPackagesEntity removed in DB-8b — see ItemPackageDbEntity + ItemPackageEntryDbEntity.
// ItemGroupDbEntity removed in DB-8b — see ItemGroupCatalogDbEntity + ItemGroupCatalogEntryDbEntity.
public class ItemEnchantEntity : PayloadIntKeyEntity { } // enchant_id
public class ItemReformEntity : PayloadStringKeyEntity { } // item_aegis
public class LaphineSynthesisEntity : PayloadStringKeyEntity { } // recipe_item
public class LaphineUpgradeEntity : PayloadStringKeyEntity { } // upgrade_item
public class RefineEntity : PayloadStringKeyEntity { } // refine_group
public class EnchantGradeEntity : PayloadStringKeyEntity { } // equip_type
public class MapDropsEntity : PayloadStringKeyEntity { } // map_name
public class MobItemRatioEntity : PayloadStringKeyEntity { } // item_aegis
// ItemCashEntity removed in DB-8b — see ItemCashDbEntity + ItemCashEntryDbEntity.
// AttendanceDbEntity (payload) removed in DB-8b — see AttendanceCatalogDbEntity + AttendanceCatalogRewardDbEntity.
// ReputationGroupEntity removed in DB-8a — replaced by typed
// ReputationGroupDbEntity + ReputationGroupMemberDbEntity.
