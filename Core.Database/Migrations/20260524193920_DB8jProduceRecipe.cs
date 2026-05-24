using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class DB8jProduceRecipe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "produce_recipe_db",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    produce_item_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    item_lv = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    require_skill_id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    require_skill_lv = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_produce_recipe_db", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "produce_recipe_material_db",
                columns: table => new
                {
                    recipe_id = table.Column<int>(type: "int", nullable: false),
                    slot_index = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    material_item_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    material_amount = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_produce_recipe_material_db", x => new { x.recipe_id, x.slot_index });
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "produce_recipe_db");

            migrationBuilder.DropTable(
                name: "produce_recipe_material_db");
        }
    }
}
