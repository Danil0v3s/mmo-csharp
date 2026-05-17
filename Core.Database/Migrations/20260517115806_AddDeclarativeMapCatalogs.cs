using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDeclarativeMapCatalogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "map_flag",
                columns: table => new
                {
                    flag_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    map_name = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    flag = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    value = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_map_flag", x => x.flag_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mob_spawn",
                columns: table => new
                {
                    spawn_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    map_name = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    center_x = table.Column<short>(type: "smallint", nullable: false),
                    center_y = table.Column<short>(type: "smallint", nullable: false),
                    span_xs = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    span_ys = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    is_boss = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    display_name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mob_id = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<int>(type: "int", nullable: false),
                    delay1 = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    delay2 = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    event_label = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    size = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ai = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mob_spawn", x => x.spawn_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "warp",
                columns: table => new
                {
                    warp_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    src_map = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    src_x = table.Column<short>(type: "smallint", nullable: false),
                    src_y = table.Column<short>(type: "smallint", nullable: false),
                    src_dir = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    warp_type = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false, defaultValue: "warp")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    span_xs = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    span_ys = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    dst_map = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dst_x = table.Column<short>(type: "smallint", nullable: false),
                    dst_y = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warp", x => x.warp_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_map_flag_map",
                table: "map_flag",
                column: "map_name");

            migrationBuilder.CreateIndex(
                name: "ix_mob_spawn_map",
                table: "mob_spawn",
                column: "map_name");

            migrationBuilder.CreateIndex(
                name: "ix_warp_src",
                table: "warp",
                columns: new[] { "src_map", "src_x", "src_y" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "map_flag");

            migrationBuilder.DropTable(
                name: "mob_spawn");

            migrationBuilder.DropTable(
                name: "warp");
        }
    }
}
