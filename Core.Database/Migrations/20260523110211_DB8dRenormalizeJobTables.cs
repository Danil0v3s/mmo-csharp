using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class DB8dRenormalizeJobTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_basepoints");

            migrationBuilder.DropTable(
                name: "job_exp");

            migrationBuilder.DropTable(
                name: "job_stats");

            migrationBuilder.CreateTable(
                name: "job_base_points_db",
                columns: table => new
                {
                    job_aegis = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    level = table.Column<int>(type: "int", nullable: false),
                    hp = table.Column<int>(type: "int", nullable: false),
                    sp = table.Column<int>(type: "int", nullable: false),
                    ap = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_base_points_db", x => new { x.job_aegis, x.level });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "job_bonus_stats_db",
                columns: table => new
                {
                    job_aegis = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    level = table.Column<int>(type: "int", nullable: false),
                    str = table.Column<int>(type: "int", nullable: false),
                    agi = table.Column<int>(type: "int", nullable: false),
                    vit = table.Column<int>(type: "int", nullable: false),
                    int_stat = table.Column<int>(type: "int", nullable: false),
                    dex = table.Column<int>(type: "int", nullable: false),
                    luk = table.Column<int>(type: "int", nullable: false),
                    pow = table.Column<int>(type: "int", nullable: false),
                    sta = table.Column<int>(type: "int", nullable: false),
                    wis = table.Column<int>(type: "int", nullable: false),
                    spl = table.Column<int>(type: "int", nullable: false),
                    con = table.Column<int>(type: "int", nullable: false),
                    crt = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_bonus_stats_db", x => new { x.job_aegis, x.level });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "job_exp_db",
                columns: table => new
                {
                    job_aegis = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    level = table.Column<int>(type: "int", nullable: false),
                    base_exp = table.Column<long>(type: "bigint", nullable: true),
                    job_exp = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_exp_db", x => new { x.job_aegis, x.level });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "job_info_db",
                columns: table => new
                {
                    job_aegis = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    max_weight = table.Column<int>(type: "int", nullable: false),
                    hp_factor = table.Column<int>(type: "int", nullable: false),
                    hp_increase = table.Column<int>(type: "int", nullable: false),
                    sp_factor = table.Column<int>(type: "int", nullable: false),
                    sp_increase = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_info_db", x => x.job_aegis);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "job_max_level_db",
                columns: table => new
                {
                    job_aegis = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    max_base_level = table.Column<int>(type: "int", nullable: true),
                    max_job_level = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_max_level_db", x => x.job_aegis);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_base_points_db");

            migrationBuilder.DropTable(
                name: "job_bonus_stats_db");

            migrationBuilder.DropTable(
                name: "job_exp_db");

            migrationBuilder.DropTable(
                name: "job_info_db");

            migrationBuilder.DropTable(
                name: "job_max_level_db");

            migrationBuilder.CreateTable(
                name: "job_basepoints",
                columns: table => new
                {
                    row_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    payload_json = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_basepoints", x => x.row_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "job_exp",
                columns: table => new
                {
                    row_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    payload_json = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_exp", x => x.row_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "job_stats",
                columns: table => new
                {
                    row_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    payload_json = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_stats", x => x.row_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
