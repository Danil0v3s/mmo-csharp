using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Database.Migrations
{
    [DbContext(typeof(Context.GameDbContext))]
    [Migration("20260228135500_AddStoragePayloadPersistence")]
    public partial class AddStoragePayloadPersistence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_storage_payload",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "int", nullable: false),
                    data = table.Column<byte[]>(type: "longblob", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_storage_payload", x => x.account_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "guild_storage_payload",
                columns: table => new
                {
                    guild_id = table.Column<int>(type: "int", nullable: false),
                    data = table.Column<byte[]>(type: "longblob", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_storage_payload", x => x.guild_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_storage_payload");

            migrationBuilder.DropTable(
                name: "guild_storage_payload");
        }
    }
}
