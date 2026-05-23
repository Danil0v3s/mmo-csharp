using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMercHomunSkillChildTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "homunculus_skill_tree_db",
                columns: table => new
                {
                    class_aegis = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    skill_id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    skill_aegis = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    max_level = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    required_level = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    required_intimacy = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    require_evolution = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_homunculus_skill_tree_db", x => new { x.class_aegis, x.skill_id });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mercenary_skill_db",
                columns: table => new
                {
                    merc_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    skill_id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    skill_aegis = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    max_level = table.Column<ushort>(type: "smallint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mercenary_skill_db", x => new { x.merc_id, x.skill_id });
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "homunculus_skill_tree_db");

            migrationBuilder.DropTable(
                name: "mercenary_skill_db");
        }
    }
}
