using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class SkillDbRequirementsColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ammo",
                table: "skill_db",
                type: "varchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "ammo_amount",
                table: "skill_db",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "inf2",
                table: "skill_db",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "unit_flags",
                table: "skill_db",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ammo",
                table: "skill_db");

            migrationBuilder.DropColumn(
                name: "ammo_amount",
                table: "skill_db");

            migrationBuilder.DropColumn(
                name: "inf2",
                table: "skill_db");

            migrationBuilder.DropColumn(
                name: "unit_flags",
                table: "skill_db");
        }
    }
}
