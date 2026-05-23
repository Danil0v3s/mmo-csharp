using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class DB8hRenormalizeRefineGrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "enchantgrade");

            migrationBuilder.DropTable(
                name: "refine");

            migrationBuilder.CreateTable(
                name: "enchantgrade_chance_db",
                columns: table => new
                {
                    equip_type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    item_level = table.Column<int>(type: "int", nullable: false),
                    grade = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    refine = table.Column<int>(type: "int", nullable: false),
                    chance = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enchantgrade_chance_db", x => new { x.equip_type, x.item_level, x.grade, x.refine });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "enchantgrade_db",
                columns: table => new
                {
                    equip_type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enchantgrade_db", x => x.equip_type);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "enchantgrade_level_db",
                columns: table => new
                {
                    equip_type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    item_level = table.Column<int>(type: "int", nullable: false),
                    grade = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enchantgrade_level_db", x => new { x.equip_type, x.item_level, x.grade });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "refine_chance_db",
                columns: table => new
                {
                    group_name = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    item_level = table.Column<int>(type: "int", nullable: false),
                    refine_level = table.Column<int>(type: "int", nullable: false),
                    chance_type = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    rate = table.Column<int>(type: "int", nullable: false),
                    price = table.Column<int>(type: "int", nullable: false),
                    material_aegis = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refine_chance_db", x => new { x.group_name, x.item_level, x.refine_level, x.chance_type });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "refine_group_db",
                columns: table => new
                {
                    group_name = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refine_group_db", x => x.group_name);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "refine_level_db",
                columns: table => new
                {
                    group_name = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    item_level = table.Column<int>(type: "int", nullable: false),
                    refine_level = table.Column<int>(type: "int", nullable: false),
                    bonus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refine_level_db", x => new { x.group_name, x.item_level, x.refine_level });
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "enchantgrade_chance_db");

            migrationBuilder.DropTable(
                name: "enchantgrade_db");

            migrationBuilder.DropTable(
                name: "enchantgrade_level_db");

            migrationBuilder.DropTable(
                name: "refine_chance_db");

            migrationBuilder.DropTable(
                name: "refine_group_db");

            migrationBuilder.DropTable(
                name: "refine_level_db");

            migrationBuilder.CreateTable(
                name: "enchantgrade",
                columns: table => new
                {
                    equip_type = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    payload_json = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enchantgrade", x => x.equip_type);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "refine",
                columns: table => new
                {
                    refine_group = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    payload_json = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refine", x => x.refine_group);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
