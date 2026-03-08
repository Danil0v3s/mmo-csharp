using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class DropCharPartyGuildClanForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_char_clan_clan_id",
                table: "char");

            migrationBuilder.DropForeignKey(
                name: "FK_char_guild_guild_id",
                table: "char");

            migrationBuilder.DropForeignKey(
                name: "FK_char_party_party_id",
                table: "char");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_char_clan_clan_id",
                table: "char",
                column: "clan_id",
                principalTable: "clan",
                principalColumn: "clan_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_char_guild_guild_id",
                table: "char",
                column: "guild_id",
                principalTable: "guild",
                principalColumn: "guild_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_char_party_party_id",
                table: "char",
                column: "party_id",
                principalTable: "party",
                principalColumn: "party_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
