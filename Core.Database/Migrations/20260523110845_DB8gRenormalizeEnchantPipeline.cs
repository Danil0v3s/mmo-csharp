using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class DB8gRenormalizeEnchantPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_enchant");

            migrationBuilder.DropTable(
                name: "item_randomopt_group");

            migrationBuilder.DropTable(
                name: "item_reform");

            migrationBuilder.DropTable(
                name: "laphine_synthesis");

            migrationBuilder.DropTable(
                name: "laphine_upgrade");

            migrationBuilder.CreateTable(
                name: "item_enchant_db",
                columns: table => new
                {
                    enchant_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    minimum_refine = table.Column<int>(type: "int", nullable: false),
                    reset_chance = table.Column<int>(type: "int", nullable: false),
                    reset_price = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_enchant_db", x => x.enchant_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "item_enchant_material_db",
                columns: table => new
                {
                    enchant_id = table.Column<int>(type: "int", nullable: false),
                    slot = table.Column<int>(type: "int", nullable: false),
                    material_aegis = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    amount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_enchant_material_db", x => new { x.enchant_id, x.slot, x.material_aegis });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "item_enchant_option_db",
                columns: table => new
                {
                    enchant_id = table.Column<int>(type: "int", nullable: false),
                    slot = table.Column<int>(type: "int", nullable: false),
                    enchant_grade = table.Column<int>(type: "int", nullable: false),
                    option_item_aegis = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    chance = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_enchant_option_db", x => new { x.enchant_id, x.slot, x.enchant_grade, x.option_item_aegis });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "item_enchant_slot_db",
                columns: table => new
                {
                    enchant_id = table.Column<int>(type: "int", nullable: false),
                    slot = table.Column<int>(type: "int", nullable: false),
                    price = table.Column<int>(type: "int", nullable: false),
                    order_index = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_enchant_slot_db", x => new { x.enchant_id, x.slot });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "item_enchant_target_db",
                columns: table => new
                {
                    enchant_id = table.Column<int>(type: "int", nullable: false),
                    item_aegis = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_enchant_target_db", x => new { x.enchant_id, x.item_aegis });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "item_randomopt_group_db",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    group_name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_randomopt_group_db", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "item_randomopt_group_option_db",
                columns: table => new
                {
                    group_id = table.Column<int>(type: "int", nullable: false),
                    slot = table.Column<int>(type: "int", nullable: false),
                    option_name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    min_value = table.Column<int>(type: "int", nullable: false),
                    max_value = table.Column<int>(type: "int", nullable: false),
                    chance = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_randomopt_group_option_db", x => new { x.group_id, x.slot, x.option_name });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "item_reform_base_db",
                columns: table => new
                {
                    result_item_aegis = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    base_item_aegis = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    maximum_refine = table.Column<int>(type: "int", nullable: true),
                    change_refine = table.Column<int>(type: "int", nullable: true),
                    result_item_override = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    random_option_group = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    clear_slots = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    remove_enchantgrade = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    cards_allowed = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_reform_base_db", x => new { x.result_item_aegis, x.base_item_aegis });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "item_reform_db",
                columns: table => new
                {
                    result_item_aegis = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_reform_db", x => x.result_item_aegis);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "laphine_synthesis_db",
                columns: table => new
                {
                    recipe_item = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reward_group = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    required_requirements_count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_laphine_synthesis_db", x => x.recipe_item);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "laphine_synthesis_requirement_db",
                columns: table => new
                {
                    recipe_item = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    requirement_item = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    refine_min = table.Column<int>(type: "int", nullable: true),
                    refine_max = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_laphine_synthesis_requirement_db", x => new { x.recipe_item, x.requirement_item });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "laphine_upgrade_db",
                columns: table => new
                {
                    upgrade_item = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    random_option_group = table.Column<int>(type: "int", nullable: true),
                    minimum_refine = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_laphine_upgrade_db", x => x.upgrade_item);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "laphine_upgrade_target_db",
                columns: table => new
                {
                    upgrade_item = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    target_item = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_laphine_upgrade_target_db", x => new { x.upgrade_item, x.target_item });
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_enchant_db");

            migrationBuilder.DropTable(
                name: "item_enchant_material_db");

            migrationBuilder.DropTable(
                name: "item_enchant_option_db");

            migrationBuilder.DropTable(
                name: "item_enchant_slot_db");

            migrationBuilder.DropTable(
                name: "item_enchant_target_db");

            migrationBuilder.DropTable(
                name: "item_randomopt_group_db");

            migrationBuilder.DropTable(
                name: "item_randomopt_group_option_db");

            migrationBuilder.DropTable(
                name: "item_reform_base_db");

            migrationBuilder.DropTable(
                name: "item_reform_db");

            migrationBuilder.DropTable(
                name: "laphine_synthesis_db");

            migrationBuilder.DropTable(
                name: "laphine_synthesis_requirement_db");

            migrationBuilder.DropTable(
                name: "laphine_upgrade_db");

            migrationBuilder.DropTable(
                name: "laphine_upgrade_target_db");

            migrationBuilder.CreateTable(
                name: "item_enchant",
                columns: table => new
                {
                    enchant_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    payload_json = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_enchant", x => x.enchant_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "item_randomopt_group",
                columns: table => new
                {
                    group_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    payload_json = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_randomopt_group", x => x.group_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "item_reform",
                columns: table => new
                {
                    item_aegis = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    payload_json = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_reform", x => x.item_aegis);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "laphine_synthesis",
                columns: table => new
                {
                    recipe_item = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    payload_json = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_laphine_synthesis", x => x.recipe_item);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "laphine_upgrade",
                columns: table => new
                {
                    upgrade_item = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    payload_json = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_laphine_upgrade", x => x.upgrade_item);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
