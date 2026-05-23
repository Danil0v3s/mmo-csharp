namespace Core.Database.Entities;

/// <summary>
/// Attendance event window (rAthena <c>db/attendance.yml</c>). One
/// row per event campaign with Start / End ISO dates. Rewards live
/// in <see cref="AttendanceCatalogRewardDbEntity"/>.
///
/// DB-8b wave replaces the prior <c>AttendanceDbEntity :
/// PayloadIntKeyEntity</c> JSON blob with typed columns. Renamed to
/// AttendanceCatalog* to avoid collision with the per-PC
/// AttendanceEntity (runtime claim state).
/// </summary>
public class AttendanceCatalogDbEntity
{
    /// <summary>Surrogate id (one per event campaign; 1..n).</summary>
    public int AttendanceId { get; set; }

    /// <summary>Event start date as YYYYMMDD integer.</summary>
    public int StartDate { get; set; }

    /// <summary>Event end date as YYYYMMDD integer.</summary>
    public int EndDate { get; set; }
}

/// <summary>
/// Per-day reward in an attendance event. Composite key
/// (AttendanceId, Day).
/// </summary>
public class AttendanceCatalogRewardDbEntity
{
    /// <summary>FK to <see cref="AttendanceCatalogDbEntity.AttendanceId"/>.</summary>
    public int AttendanceId { get; set; }

    /// <summary>Day index (1-based, up to 28 in rAthena stock).</summary>
    public int Day { get; set; }

    /// <summary>Reward item id (rAthena <c>item_db.id</c>).</summary>
    public int ItemId { get; set; }

    /// <summary>Amount granted (rAthena default 1 when omitted).</summary>
    public int Amount { get; set; } = 1;
}
