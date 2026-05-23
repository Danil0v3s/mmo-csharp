using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddAtGCatalogTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "achievement_level_db",
                columns: table => new
                {
                    level = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    required_points = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievement_level_db", x => x.level);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "const_db",
                columns: table => new
                {
                    name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    value = table.Column<long>(type: "bigint", nullable: false),
                    is_parameter = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_const_db", x => x.name);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "job_aspd_db",
                columns: table => new
                {
                    job_aegis = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    weapon_type = table.Column<int>(type: "int", nullable: false),
                    base_delay_ms = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_aspd_db", x => new { x.job_aegis, x.weapon_type });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "stylist_db",
                columns: table => new
                {
                    look = table.Column<int>(type: "int", nullable: false),
                    client_index = table.Column<int>(type: "int", nullable: false),
                    doram_only = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    value = table.Column<int>(type: "int", nullable: false),
                    cost_zeny = table.Column<int>(type: "int", nullable: false),
                    required_item_aegis = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    required_item_box_aegis = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stylist_db", x => new { x.look, x.client_index, x.doram_only });
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "achievement_level_db");

            migrationBuilder.DropTable(
                name: "const_db");

            migrationBuilder.DropTable(
                name: "job_aspd_db");

            migrationBuilder.DropTable(
                name: "stylist_db");
        }
    }
}
