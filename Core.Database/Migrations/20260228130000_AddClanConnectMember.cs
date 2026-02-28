using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Database.Migrations
{
    [DbContext(typeof(Context.GameDbContext))]
    [Migration("20260228130000_AddClanConnectMember")]
    public partial class AddClanConnectMember : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ushort>(
                name: "connect_member",
                table: "clan",
                type: "smallint unsigned",
                nullable: false,
                defaultValue: (ushort)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "connect_member",
                table: "clan");
        }
    }
}
