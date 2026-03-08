using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCharShadowFkNavigations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_char_clan_ClanEntityClanId",
                table: "char");

            migrationBuilder.DropForeignKey(
                name: "FK_char_guild_GuildEntityGuildId",
                table: "char");

            migrationBuilder.DropForeignKey(
                name: "FK_char_party_PartyEntityPartyId",
                table: "char");

            migrationBuilder.DropIndex(
                name: "IX_char_ClanEntityClanId",
                table: "char");

            migrationBuilder.DropIndex(
                name: "IX_char_GuildEntityGuildId",
                table: "char");

            migrationBuilder.DropIndex(
                name: "IX_char_PartyEntityPartyId",
                table: "char");

            migrationBuilder.DropColumn(
                name: "ClanEntityClanId",
                table: "char");

            migrationBuilder.DropColumn(
                name: "GuildEntityGuildId",
                table: "char");

            migrationBuilder.DropColumn(
                name: "PartyEntityPartyId",
                table: "char");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClanEntityClanId",
                table: "char",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GuildEntityGuildId",
                table: "char",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PartyEntityPartyId",
                table: "char",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_char_ClanEntityClanId",
                table: "char",
                column: "ClanEntityClanId");

            migrationBuilder.CreateIndex(
                name: "IX_char_GuildEntityGuildId",
                table: "char",
                column: "GuildEntityGuildId");

            migrationBuilder.CreateIndex(
                name: "IX_char_PartyEntityPartyId",
                table: "char",
                column: "PartyEntityPartyId");

            migrationBuilder.AddForeignKey(
                name: "FK_char_clan_ClanEntityClanId",
                table: "char",
                column: "ClanEntityClanId",
                principalTable: "clan",
                principalColumn: "clan_id");

            migrationBuilder.AddForeignKey(
                name: "FK_char_guild_GuildEntityGuildId",
                table: "char",
                column: "GuildEntityGuildId",
                principalTable: "guild",
                principalColumn: "guild_id");

            migrationBuilder.AddForeignKey(
                name: "FK_char_party_PartyEntityPartyId",
                table: "char",
                column: "PartyEntityPartyId",
                principalTable: "party",
                principalColumn: "party_id");
        }
    }
}
