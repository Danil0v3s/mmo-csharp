using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class QuestObjectiveFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "element1",
                table: "quest_db",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "element2",
                table: "quest_db",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "element3",
                table: "quest_db",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "location1",
                table: "quest_db",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "location2",
                table: "quest_db",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "location3",
                table: "quest_db",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "max_level1",
                table: "quest_db",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "max_level2",
                table: "quest_db",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "max_level3",
                table: "quest_db",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "min_level1",
                table: "quest_db",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "min_level2",
                table: "quest_db",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "min_level3",
                table: "quest_db",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "mobs_allowed1",
                table: "quest_db",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "mobs_allowed2",
                table: "quest_db",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "mobs_allowed3",
                table: "quest_db",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "race1",
                table: "quest_db",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "race2",
                table: "quest_db",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "race3",
                table: "quest_db",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "size1",
                table: "quest_db",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "size2",
                table: "quest_db",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "size3",
                table: "quest_db",
                type: "varchar(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "element1",
                table: "quest_db");

            migrationBuilder.DropColumn(
                name: "element2",
                table: "quest_db");

            migrationBuilder.DropColumn(
                name: "element3",
                table: "quest_db");

            migrationBuilder.DropColumn(
                name: "location1",
                table: "quest_db");

            migrationBuilder.DropColumn(
                name: "location2",
                table: "quest_db");

            migrationBuilder.DropColumn(
                name: "location3",
                table: "quest_db");

            migrationBuilder.DropColumn(
                name: "max_level1",
                table: "quest_db");

            migrationBuilder.DropColumn(
                name: "max_level2",
                table: "quest_db");

            migrationBuilder.DropColumn(
                name: "max_level3",
                table: "quest_db");

            migrationBuilder.DropColumn(
                name: "min_level1",
                table: "quest_db");

            migrationBuilder.DropColumn(
                name: "min_level2",
                table: "quest_db");

            migrationBuilder.DropColumn(
                name: "min_level3",
                table: "quest_db");

            migrationBuilder.DropColumn(
                name: "mobs_allowed1",
                table: "quest_db");

            migrationBuilder.DropColumn(
                name: "mobs_allowed2",
                table: "quest_db");

            migrationBuilder.DropColumn(
                name: "mobs_allowed3",
                table: "quest_db");

            migrationBuilder.DropColumn(
                name: "race1",
                table: "quest_db");

            migrationBuilder.DropColumn(
                name: "race2",
                table: "quest_db");

            migrationBuilder.DropColumn(
                name: "race3",
                table: "quest_db");

            migrationBuilder.DropColumn(
                name: "size1",
                table: "quest_db");

            migrationBuilder.DropColumn(
                name: "size2",
                table: "quest_db");

            migrationBuilder.DropColumn(
                name: "size3",
                table: "quest_db");
        }
    }
}
