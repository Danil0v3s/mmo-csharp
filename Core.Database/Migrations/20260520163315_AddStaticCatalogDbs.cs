using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddStaticCatalogDbs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "abra_db",
                columns: table => new
                {
                    skill_name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_abra_db", x => x.skill_name);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "achievement_db",
                columns: table => new
                {
                    achievement_id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    group_name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    score = table.Column<int>(type: "int", nullable: false),
                    dependents = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    targets = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievement_db", x => x.achievement_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "homunculus_db",
                columns: table => new
                {
                    class_aegis = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    food_item = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hungry_delay = table.Column<int>(type: "int", nullable: true),
                    size = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    race = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    element = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ele_level = table.Column<int>(type: "int", nullable: true),
                    attack_range = table.Column<int>(type: "int", nullable: true),
                    evolution_class = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_homunculus_db", x => x.class_aegis);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "instance_db",
                columns: table => new
                {
                    instance_id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    time_limit = table.Column<int>(type: "int", nullable: false),
                    idle_timeout = table.Column<int>(type: "int", nullable: false),
                    enter_map = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    enter_x = table.Column<int>(type: "int", nullable: true),
                    enter_y = table.Column<int>(type: "int", nullable: true),
                    additional_maps = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_instance_db", x => x.instance_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "magicmushroom_db",
                columns: table => new
                {
                    skill_name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_magicmushroom_db", x => x.skill_name);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mercenary_db",
                columns: table => new
                {
                    merc_id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    aegis_name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    level = table.Column<int>(type: "int", nullable: true),
                    hp = table.Column<int>(type: "int", nullable: true),
                    sp = table.Column<int>(type: "int", nullable: true),
                    attack = table.Column<int>(type: "int", nullable: true),
                    attack2 = table.Column<int>(type: "int", nullable: true),
                    defense = table.Column<int>(type: "int", nullable: true),
                    magic_defense = table.Column<int>(type: "int", nullable: true),
                    str = table.Column<int>(type: "int", nullable: true),
                    agi = table.Column<int>(type: "int", nullable: true),
                    vit = table.Column<int>(type: "int", nullable: true),
                    intel = table.Column<int>(type: "int", nullable: true),
                    dex = table.Column<int>(type: "int", nullable: true),
                    luk = table.Column<int>(type: "int", nullable: true),
                    attack_range = table.Column<int>(type: "int", nullable: true),
                    skill_range = table.Column<int>(type: "int", nullable: true),
                    chase_range = table.Column<int>(type: "int", nullable: true),
                    size = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    race = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    element = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ele_level = table.Column<int>(type: "int", nullable: true),
                    walk_speed = table.Column<int>(type: "int", nullable: true),
                    attack_delay = table.Column<int>(type: "int", nullable: true),
                    attack_motion = table.Column<int>(type: "int", nullable: true),
                    damage_motion = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mercenary_db", x => x.merc_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pet_db",
                columns: table => new
                {
                    mob_aegis = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tame_item = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    egg_item = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    equip_item = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    food_item = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fullness = table.Column<int>(type: "int", nullable: true),
                    hunger_delay = table.Column<int>(type: "int", nullable: true),
                    intimacy_start = table.Column<int>(type: "int", nullable: true),
                    intimacy_fed = table.Column<int>(type: "int", nullable: true),
                    intimacy_overfed = table.Column<int>(type: "int", nullable: true),
                    intimacy_hungry = table.Column<int>(type: "int", nullable: true),
                    intimacy_owner_die = table.Column<int>(type: "int", nullable: true),
                    capture_rate = table.Column<int>(type: "int", nullable: true),
                    special_performance = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    attack_rate = table.Column<int>(type: "int", nullable: true),
                    retaliate_rate = table.Column<int>(type: "int", nullable: true),
                    change_target_rate = table.Column<int>(type: "int", nullable: true),
                    allow_auto_feed = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    script = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    support_script = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pet_db", x => x.mob_aegis);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "quest_db",
                columns: table => new
                {
                    quest_id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    title = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    time_limit = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mob1 = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    count1 = table.Column<int>(type: "int", nullable: false),
                    mob2 = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    count2 = table.Column<int>(type: "int", nullable: false),
                    mob3 = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    count3 = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quest_db", x => x.quest_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "spellbook_db",
                columns: table => new
                {
                    book_name_aegis = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    skill_name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    preserve_points = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_spellbook_db", x => x.book_name_aegis);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "abra_db");

            migrationBuilder.DropTable(
                name: "achievement_db");

            migrationBuilder.DropTable(
                name: "homunculus_db");

            migrationBuilder.DropTable(
                name: "instance_db");

            migrationBuilder.DropTable(
                name: "magicmushroom_db");

            migrationBuilder.DropTable(
                name: "mercenary_db");

            migrationBuilder.DropTable(
                name: "pet_db");

            migrationBuilder.DropTable(
                name: "quest_db");

            migrationBuilder.DropTable(
                name: "spellbook_db");
        }
    }
}
