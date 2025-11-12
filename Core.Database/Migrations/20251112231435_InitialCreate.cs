using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Core.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "acc_reg_num",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    key = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    index = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    value = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acc_reg_num", x => new { x.account_id, x.key, x.index });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "acc_reg_str",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    key = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    index = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    value = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: false, defaultValue: "0")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acc_reg_str", x => new { x.account_id, x.key, x.index });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "atcommandlog",
                columns: table => new
                {
                    atcommand_id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    atcommand_date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    account_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    char_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    char_name = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    map = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    command = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_atcommandlog", x => x.atcommand_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "auction",
                columns: table => new
                {
                    auction_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    seller_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    seller_name = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    buyer_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    buyer_name = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    price = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    buynow = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    hours = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    timestamp = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    nameid = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    item_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    refine = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    attribute = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    card0 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card1 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card2 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card3 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    option_id0 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val0 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm0 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id1 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val1 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm1 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id2 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val2 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm2 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id3 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val3 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm3 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id4 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val4 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm4 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    unique_id = table.Column<ulong>(type: "bigint unsigned", nullable: false, defaultValue: 0ul),
                    enchantgrade = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auction", x => x.auction_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "barter",
                columns: table => new
                {
                    name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    index = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    amount = table.Column<ushort>(type: "smallint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_barter", x => new { x.name, x.index });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "bonus_script",
                columns: table => new
                {
                    char_id = table.Column<int>(type: "int", nullable: false),
                    script = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tick = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    flag = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    type = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    icon = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)-1)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "branchlog",
                columns: table => new
                {
                    branch_id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    branch_date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    account_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    char_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    char_name = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    map = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branchlog", x => x.branch_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "buyingstores",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    account_id = table.Column<int>(type: "int", nullable: false),
                    char_id = table.Column<int>(type: "int", nullable: false),
                    sex = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false, defaultValue: "M")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    map = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    x = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    y = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    title = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    limit = table.Column<uint>(type: "int unsigned", nullable: false),
                    body_direction = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false, defaultValue: "4")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    head_direction = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false, defaultValue: "0")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sit = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false, defaultValue: "1")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    autotrade = table.Column<sbyte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buyingstores", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "cashlog",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    char_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    type = table.Column<string>(type: "varchar(255)", nullable: false, defaultValue: "S")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    cash_type = table.Column<string>(type: "longtext", nullable: false, defaultValue: "O")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    amount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    map = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cashlog", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "char_configs",
                columns: table => new
                {
                    world_name = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    account_id = table.Column<int>(type: "int", nullable: false),
                    char_id = table.Column<int>(type: "int", nullable: false),
                    data = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_char_configs", x => new { x.world_name, x.account_id, x.char_id });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "char_reg_num",
                columns: table => new
                {
                    char_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    key = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    index = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    value = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_char_reg_num", x => new { x.char_id, x.key, x.index });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "char_reg_str",
                columns: table => new
                {
                    char_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    key = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    index = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    value = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: false, defaultValue: "0")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_char_reg_str", x => new { x.char_id, x.key, x.index });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "charlog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    char_msg = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, defaultValue: "char select")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    account_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    char_num = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    name = table.Column<string>(type: "varchar(23)", maxLength: 23, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    str = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    agi = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    vit = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    @int = table.Column<uint>(name: "int", type: "int unsigned", nullable: false, defaultValue: 0u),
                    dex = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    luk = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    hair = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    hair_color = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_charlog", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "chatlog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    type = table.Column<string>(type: "longtext", nullable: false, defaultValue: "O")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    src_charid = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    src_accountid = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    src_map = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    src_map_x = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    src_map_y = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    dst_charname = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    message = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chatlog", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "clan",
                columns: table => new
                {
                    clan_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    master = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mapname = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    max_member = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clan", x => x.clan_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "db_roulette",
                columns: table => new
                {
                    index = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    level = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    item_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    amount = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)1),
                    flag = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_db_roulette", x => x.index);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "elemental",
                columns: table => new
                {
                    ele_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    char_id = table.Column<int>(type: "int", nullable: false),
                    @class = table.Column<uint>(name: "class", type: "int unsigned", nullable: false, defaultValue: 0u),
                    mode = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 1u),
                    hp = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    sp = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    max_hp = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    max_sp = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    atk1 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    atk2 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    matk = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    aspd = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    def = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    mdef = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    flee = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    hit = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    life_time = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_elemental", x => x.ele_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "feedinglog",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    char_id = table.Column<int>(type: "int", nullable: false),
                    target_id = table.Column<int>(type: "int", nullable: false),
                    target_class = table.Column<short>(type: "smallint", nullable: false),
                    type = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    intimacy = table.Column<uint>(type: "int unsigned", nullable: false),
                    item_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    map = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    x = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    y = table.Column<ushort>(type: "smallint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feedinglog", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "global_acc_reg_num",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    key = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    index = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    value = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_global_acc_reg_num", x => new { x.account_id, x.key, x.index });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "global_acc_reg_str",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    key = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    index = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    value = table.Column<string>(type: "varchar(254)", maxLength: 254, nullable: false, defaultValue: "0")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_global_acc_reg_str", x => new { x.account_id, x.key, x.index });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "guild",
                columns: table => new
                {
                    guild_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    name = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    char_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    master = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    guild_lv = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    connect_member = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    max_member = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    average_lv = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)1),
                    exp = table.Column<ulong>(type: "bigint unsigned", nullable: false, defaultValue: 0ul),
                    next_exp = table.Column<ulong>(type: "bigint unsigned", nullable: false, defaultValue: 0ul),
                    skill_point = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    mes1 = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mes2 = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    emblem_len = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    emblem_id = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    emblem_data = table.Column<byte[]>(type: "longblob", nullable: true),
                    last_master_change = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild", x => x.guild_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "guild_castle",
                columns: table => new
                {
                    castle_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    guild_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    economy = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    defense = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    triggerE = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    triggerD = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    nextTime = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    payTime = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    createTime = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    visibleC = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    visibleG0 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    visibleG1 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    visibleG2 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    visibleG3 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    visibleG4 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    visibleG5 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    visibleG6 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    visibleG7 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_castle", x => x.castle_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "guild_emblems",
                columns: table => new
                {
                    world_name = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    guild_id = table.Column<int>(type: "int", nullable: false),
                    file_type = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    file_data = table.Column<byte[]>(type: "longblob", nullable: true),
                    version = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_emblems", x => new { x.world_name, x.guild_id });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "guild_expulsion",
                columns: table => new
                {
                    guild_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    name = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    account_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    mes = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    char_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_expulsion", x => new { x.guild_id, x.name });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "guild_storage_log",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guild_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    char_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    name = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nameid = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    amount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    identify = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    refine = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    attribute = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    card0 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card1 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card2 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card3 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    option_id0 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val0 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm0 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id1 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val1 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm1 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id2 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val2 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm2 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id3 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val3 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm3 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id4 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val4 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm4 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    expire_time = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    unique_id = table.Column<ulong>(type: "bigint unsigned", nullable: false, defaultValue: 0ul),
                    bound = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    enchantgrade = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_storage_log", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "homunculus",
                columns: table => new
                {
                    homun_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    char_id = table.Column<int>(type: "int", nullable: false),
                    @class = table.Column<uint>(name: "class", type: "int unsigned", nullable: false, defaultValue: 0u),
                    prev_class = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    name = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    level = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    exp = table.Column<ulong>(type: "bigint unsigned", nullable: false, defaultValue: 0ul),
                    intimacy = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    hunger = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    str = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    agi = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    vit = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    @int = table.Column<ushort>(name: "int", type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    dex = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    luk = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    hp = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    max_hp = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    sp = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    max_sp = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    skill_point = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    alive = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    rename_flag = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    vaporize = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    autofeed = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_homunculus", x => x.homun_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "interlog",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    log = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interlog", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ipbanlist",
                columns: table => new
                {
                    list = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    btime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    rtime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    reason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ipbanlist", x => new { x.list, x.btime });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "item_db",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name_aegis = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_english = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    subtype = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    price_buy = table.Column<uint>(type: "int unsigned", nullable: true),
                    price_sell = table.Column<uint>(type: "int unsigned", nullable: true),
                    weight = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    attack = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    magic_attack = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    defense = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    range = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    slots = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_all = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_acolyte = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_alchemist = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_archer = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_assassin = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_barddancer = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_blacksmith = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_crusader = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_gunslinger = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_hunter = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_kagerouoboro = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_knight = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_mage = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_merchant = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_monk = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_ninja = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_novice = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_priest = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_rebellion = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_rogue = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_sage = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_soullinker = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_spirit_handler = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_stargladiator = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_summoner = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_supernovice = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_swordman = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_taekwon = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_thief = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    job_wizard = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    class_all = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    class_normal = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    class_upper = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    class_baby = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    class_third = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    class_third_upper = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    class_third_baby = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    class_fourth = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    gender = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    location_head_top = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    location_head_mid = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    location_head_low = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    location_armor = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    location_right_hand = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    location_left_hand = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    location_garment = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    location_shoes = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    location_right_accessory = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    location_left_accessory = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    location_costume_head_top = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    location_costume_head_mid = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    location_costume_head_low = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    location_costume_garment = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    location_ammo = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    location_shadow_armor = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    location_shadow_weapon = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    location_shadow_shield = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    location_shadow_shoes = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    location_shadow_right_accessory = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    location_shadow_left_accessory = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    weapon_level = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    armor_level = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    equip_level_min = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    equip_level_max = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    refineable = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    gradable = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    view = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    alias_name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    flag_buyingstore = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    flag_deadbranch = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    flag_container = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    flag_uniqueid = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    flag_bindonequip = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    flag_dropannounce = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    flag_noconsume = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    flag_dropeffect = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    delay_duration = table.Column<ulong>(type: "bigint unsigned", nullable: true),
                    delay_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    stack_amount = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    stack_inventory = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    stack_cart = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    stack_storage = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    stack_guildstorage = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    nouse_override = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    nouse_sitting = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    trade_override = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    trade_nodrop = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    trade_notrade = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    trade_tradepartner = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    trade_nosell = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    trade_nocart = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    trade_nostorage = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    trade_noguildstorage = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    trade_nomail = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    trade_noauction = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    script = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    equip_script = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    unequip_script = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item_db", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "login",
                columns: table => new
                {
                    account_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    userid = table.Column<string>(type: "varchar(23)", maxLength: 23, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_pass = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sex = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(39)", maxLength: 39, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    group_id = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    state = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    unban_time = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    expiration_time = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    logincount = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    lastlogin = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_ip = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false, defaultValue: "0")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    birthdate = table.Column<DateOnly>(type: "date", nullable: true),
                    character_slots = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    pincode = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: false, defaultValue: "0")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pincode_change = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    vip_time = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    old_group = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    web_auth_token = table.Column<string>(type: "varchar(17)", maxLength: 17, nullable: true, defaultValue: "0")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    web_auth_token_enabled = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_login", x => x.account_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "loginlog",
                columns: table => new
                {
                    time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ip = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user = table.Column<string>(type: "varchar(23)", maxLength: 23, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    rcode = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    log = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mail",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    send_name = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    send_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    dest_name = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dest_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    title = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    message = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    time = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    zeny = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    type = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mail", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mapreg",
                columns: table => new
                {
                    varname = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    index = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    value = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mapreg", x => new { x.varname, x.index });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "market",
                columns: table => new
                {
                    name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nameid = table.Column<uint>(type: "int unsigned", nullable: false),
                    price = table.Column<uint>(type: "int unsigned", nullable: false),
                    amount = table.Column<int>(type: "int", nullable: false),
                    flag = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_market", x => new { x.name, x.nameid });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mercenary",
                columns: table => new
                {
                    mer_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    char_id = table.Column<int>(type: "int", nullable: false),
                    @class = table.Column<uint>(name: "class", type: "int unsigned", nullable: false, defaultValue: 0u),
                    hp = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    sp = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    kill_counter = table.Column<int>(type: "int", nullable: false),
                    life_time = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mercenary", x => x.mer_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mercenary_owner",
                columns: table => new
                {
                    char_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    merc_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    arch_calls = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    arch_faith = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    spear_calls = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    spear_faith = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    sword_calls = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    sword_faith = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mercenary_owner", x => x.char_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "merchant_configs",
                columns: table => new
                {
                    world_name = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    account_id = table.Column<int>(type: "int", nullable: false),
                    char_id = table.Column<int>(type: "int", nullable: false),
                    store_type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    data = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_merchant_configs", x => new { x.world_name, x.account_id, x.char_id, x.store_type });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mob_db",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name_aegis = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_english = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    name_japanese = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    level = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    hp = table.Column<uint>(type: "int unsigned", nullable: true),
                    sp = table.Column<uint>(type: "int unsigned", nullable: true),
                    base_exp = table.Column<uint>(type: "int unsigned", nullable: true),
                    job_exp = table.Column<uint>(type: "int unsigned", nullable: true),
                    mvp_exp = table.Column<uint>(type: "int unsigned", nullable: true),
                    attack = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    attack2 = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    defense = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    magic_defense = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    resistance = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    magic_resistance = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    str = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    agi = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    vit = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    @int = table.Column<ushort>(name: "int", type: "smallint unsigned", nullable: true),
                    dex = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    luk = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    attack_range = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    skill_range = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    chase_range = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    size = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    race = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    racegroup_goblin = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_kobold = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_orc = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_golem = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_guardian = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_ninja = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_gvg = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_battlefield = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_treasure = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_biolab = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_manuk = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_splendide = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_scaraba = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_ogh_atk_def = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_ogh_hidden = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_bio5_swordman_thief = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_bio5_acolyte_merchant = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_bio5_mage_archer = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_bio5_mvp = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_clocktower = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_thanatos = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_faceworm = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_hearthunter = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_rockridge = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_werner_lab = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_temple_demon = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_illusion_vampire = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_malangdo = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_ep172alpha = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_ep172beta = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_ep172bath = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_illusion_turtle = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_rachel_sanctuary = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_illusion_luanda = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_illusion_frozen = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_illusion_moonlight = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_ep16_def = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_edda_arunafeltz = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_lasagna = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_glast_heim_abyss = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_destroyed_valkyrie_realm = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    racegroup_encroached_gephenia = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    element = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    element_level = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    walk_speed = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    attack_delay = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    attack_motion = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    damage_motion = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    damage_taken = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    groupid = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    title = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ai = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    @class = table.Column<string>(name: "class", type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mode_canmove = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_looter = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_aggressive = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_assist = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_castsensoridle = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_norandomwalk = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_nocast = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_canattack = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_castsensorchase = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_changechase = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_angry = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_changetargetmelee = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_changetargetchase = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_targetweak = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_randomtarget = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_ignoremelee = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_ignoremagic = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_ignoreranged = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_mvp = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_ignoremisc = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_knockbackimmune = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_teleportblock = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_fixeditemdrop = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_detector = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_statusimmune = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mode_skillimmune = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mvpdrop1_item = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mvpdrop1_rate = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    mvpdrop1_option = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mvpdrop1_index = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mvpdrop2_item = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mvpdrop2_rate = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    mvpdrop2_option = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mvpdrop2_index = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    mvpdrop3_item = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mvpdrop3_rate = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    mvpdrop3_option = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mvpdrop3_index = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    drop1_item = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drop1_rate = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    drop1_nosteal = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    drop1_option = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drop1_index = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    drop2_item = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drop2_rate = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    drop2_nosteal = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    drop2_option = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drop2_index = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    drop3_item = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drop3_rate = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    drop3_nosteal = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    drop3_option = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drop3_index = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    drop4_item = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drop4_rate = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    drop4_nosteal = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    drop4_option = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drop4_index = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    drop5_item = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drop5_rate = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    drop5_nosteal = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    drop5_option = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drop5_index = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    drop6_item = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drop6_rate = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    drop6_nosteal = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    drop6_option = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drop6_index = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    drop7_item = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drop7_rate = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    drop7_nosteal = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    drop7_option = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drop7_index = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    drop8_item = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drop8_rate = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    drop8_nosteal = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    drop8_option = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drop8_index = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    drop9_item = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drop9_rate = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    drop9_nosteal = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    drop9_option = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drop9_index = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    drop10_item = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drop10_rate = table.Column<ushort>(type: "smallint unsigned", nullable: true),
                    drop10_nosteal = table.Column<byte>(type: "tinyint unsigned", nullable: true),
                    drop10_option = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    drop10_index = table.Column<byte>(type: "tinyint unsigned", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mob_db", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mvplog",
                columns: table => new
                {
                    mvp_id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    mvp_date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    kill_char_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    monster_id = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    prize = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    mvpexp = table.Column<ulong>(type: "bigint unsigned", nullable: false, defaultValue: 0ul),
                    map = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mvplog", x => x.mvp_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "npclog",
                columns: table => new
                {
                    npc_id = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    npc_date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    account_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    char_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    char_name = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    map = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mes = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npclog", x => x.npc_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "party",
                columns: table => new
                {
                    party_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    exp = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    item = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    leader_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    leader_char = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_party", x => x.party_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "party_bookings",
                columns: table => new
                {
                    world_name = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    account_id = table.Column<int>(type: "int", nullable: false),
                    char_id = table.Column<int>(type: "int", nullable: false),
                    char_name = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    purpose = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    assist = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    damagedealer = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    healer = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    tanker = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    minimum_level = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    maximum_level = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    comment = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_party_bookings", x => new { x.world_name, x.account_id, x.char_id });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pet",
                columns: table => new
                {
                    pet_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    @class = table.Column<uint>(name: "class", type: "int unsigned", nullable: false, defaultValue: 0u),
                    name = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    account_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    char_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    level = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    egg_id = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    equip = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    intimate = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    hungry = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    rename_flag = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    incubate = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    autofeed = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pet", x => x.pet_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "picklog",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    char_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    type = table.Column<string>(type: "varchar(255)", nullable: false, defaultValue: "P")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    nameid = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    amount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    refine = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    card0 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card1 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card2 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card3 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    option_id0 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val0 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm0 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id1 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val1 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm1 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id2 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val2 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm2 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id3 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val3 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm3 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id4 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val4 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm4 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    unique_id = table.Column<ulong>(type: "bigint unsigned", nullable: false, defaultValue: 0ul),
                    map = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    bound = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    enchantgrade = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_picklog", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sales",
                columns: table => new
                {
                    nameid = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    start = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    end = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    amount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales", x => x.nameid);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sc_data",
                columns: table => new
                {
                    char_id = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    account_id = table.Column<int>(type: "int", nullable: false),
                    tick = table.Column<long>(type: "bigint", nullable: false),
                    val1 = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    val2 = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    val3 = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    val4 = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sc_data", x => new { x.char_id, x.type });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "skillcooldown",
                columns: table => new
                {
                    char_id = table.Column<int>(type: "int", nullable: false),
                    skill = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    account_id = table.Column<int>(type: "int", nullable: false),
                    tick = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skillcooldown", x => new { x.char_id, x.skill });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "skillcooldown_homunculus",
                columns: table => new
                {
                    homun_id = table.Column<int>(type: "int", nullable: false),
                    skill = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    tick = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skillcooldown_homunculus", x => new { x.homun_id, x.skill });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "skillcooldown_mercenary",
                columns: table => new
                {
                    mer_id = table.Column<int>(type: "int", nullable: false),
                    skill = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    tick = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skillcooldown_mercenary", x => new { x.mer_id, x.skill });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "storage",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    account_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    nameid = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    amount = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    equip = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    identify = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    refine = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    attribute = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    card0 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card1 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card2 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card3 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    option_id0 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val0 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm0 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id1 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val1 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm1 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id2 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val2 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm2 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id3 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val3 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm3 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id4 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val4 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm4 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    expire_time = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    bound = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    unique_id = table.Column<ulong>(type: "bigint unsigned", nullable: false, defaultValue: 0ul),
                    enchantgrade = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storage", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user_configs",
                columns: table => new
                {
                    world_name = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    account_id = table.Column<int>(type: "int", nullable: false),
                    data = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_configs", x => new { x.world_name, x.account_id });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "vendings",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    account_id = table.Column<int>(type: "int", nullable: false),
                    char_id = table.Column<int>(type: "int", nullable: false),
                    sex = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false, defaultValue: "M")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    map = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    x = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    y = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    title = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    body_direction = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false, defaultValue: "4")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    head_direction = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false, defaultValue: "0")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sit = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false, defaultValue: "1")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    autotrade = table.Column<sbyte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendings", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "zenylog",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    char_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    src_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    type = table.Column<string>(type: "varchar(255)", nullable: false, defaultValue: "S")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    amount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    map = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_zenylog", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "buyingstore_items",
                columns: table => new
                {
                    buyingstore_id = table.Column<int>(type: "int", nullable: false),
                    index = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    item_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    amount = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    price = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buyingstore_items", x => new { x.buyingstore_id, x.index });
                    table.ForeignKey(
                        name: "FK_buyingstore_items_buyingstores_buyingstore_id",
                        column: x => x.buyingstore_id,
                        principalTable: "buyingstores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "clan_alliance",
                columns: table => new
                {
                    clan_id = table.Column<int>(type: "int", nullable: false),
                    alliance_id = table.Column<int>(type: "int", nullable: false),
                    opposition = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clan_alliance", x => new { x.clan_id, x.alliance_id });
                    table.ForeignKey(
                        name: "FK_clan_alliance_clan_clan_id",
                        column: x => x.clan_id,
                        principalTable: "clan",
                        principalColumn: "clan_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "guild_alliance",
                columns: table => new
                {
                    guild_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    alliance_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    opposition = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    name = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_alliance", x => new { x.guild_id, x.alliance_id });
                    table.ForeignKey(
                        name: "FK_guild_alliance_guild_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guild",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "guild_member",
                columns: table => new
                {
                    guild_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    char_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    exp = table.Column<ulong>(type: "bigint unsigned", nullable: false, defaultValue: 0ul),
                    position = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_member", x => new { x.guild_id, x.char_id });
                    table.ForeignKey(
                        name: "FK_guild_member_guild_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guild",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "guild_position",
                columns: table => new
                {
                    guild_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    position = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    name = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mode = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    exp_mode = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_position", x => new { x.guild_id, x.position });
                    table.ForeignKey(
                        name: "FK_guild_position_guild_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guild",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "guild_skill",
                columns: table => new
                {
                    guild_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    id = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    lv = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_skill", x => new { x.guild_id, x.id });
                    table.ForeignKey(
                        name: "FK_guild_skill_guild_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guild",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "guild_storage",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    guild_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    nameid = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    amount = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    equip = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    identify = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    refine = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    attribute = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    card0 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card1 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card2 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card3 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    option_id0 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val0 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm0 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id1 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val1 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm1 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id2 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val2 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm2 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id3 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val3 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm3 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id4 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val4 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm4 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    expire_time = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    bound = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    unique_id = table.Column<ulong>(type: "bigint unsigned", nullable: false, defaultValue: 0ul),
                    enchantgrade = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_storage", x => x.id);
                    table.ForeignKey(
                        name: "FK_guild_storage_guild_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guild",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "skill_homunculus",
                columns: table => new
                {
                    homun_id = table.Column<int>(type: "int", nullable: false),
                    id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    lv = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_homunculus", x => new { x.homun_id, x.id });
                    table.ForeignKey(
                        name: "FK_skill_homunculus_homunculus_homun_id",
                        column: x => x.homun_id,
                        principalTable: "homunculus",
                        principalColumn: "homun_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mail_attachments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    index = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    nameid = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    amount = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    refine = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    attribute = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    identify = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    card0 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card1 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card2 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card3 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    option_id0 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val0 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm0 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id1 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val1 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm1 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id2 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val2 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm2 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id3 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val3 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm3 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id4 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val4 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm4 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    unique_id = table.Column<ulong>(type: "bigint unsigned", nullable: false, defaultValue: 0ul),
                    bound = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    enchantgrade = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    MailId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mail_attachments", x => new { x.id, x.index });
                    table.ForeignKey(
                        name: "FK_mail_attachments_mail_MailId",
                        column: x => x.MailId,
                        principalTable: "mail",
                        principalColumn: "id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "char",
                columns: table => new
                {
                    char_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    account_id = table.Column<int>(type: "int", nullable: false),
                    char_num = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    name = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    @class = table.Column<ushort>(name: "class", type: "smallint unsigned", nullable: false),
                    base_level = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)1),
                    job_level = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)1),
                    base_exp = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    job_exp = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    zeny = table.Column<uint>(type: "int unsigned", nullable: false),
                    str = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    agi = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    vit = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    @int = table.Column<ushort>(name: "int", type: "smallint unsigned", nullable: false),
                    dex = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    luk = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    pow = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    sta = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    wis = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    spl = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    con = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    crt = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    max_hp = table.Column<uint>(type: "int unsigned", nullable: false),
                    hp = table.Column<uint>(type: "int unsigned", nullable: false),
                    max_sp = table.Column<uint>(type: "int unsigned", nullable: false),
                    sp = table.Column<uint>(type: "int unsigned", nullable: false),
                    max_ap = table.Column<uint>(type: "int unsigned", nullable: false),
                    ap = table.Column<uint>(type: "int unsigned", nullable: false),
                    status_point = table.Column<uint>(type: "int unsigned", nullable: false),
                    skill_point = table.Column<uint>(type: "int unsigned", nullable: false),
                    trait_point = table.Column<uint>(type: "int unsigned", nullable: false),
                    option = table.Column<int>(type: "int", nullable: false),
                    karma = table.Column<sbyte>(type: "tinyint", nullable: false),
                    manner = table.Column<short>(type: "smallint", nullable: false),
                    party_id = table.Column<int>(type: "int", nullable: false),
                    guild_id = table.Column<int>(type: "int", nullable: false),
                    pet_id = table.Column<int>(type: "int", nullable: false),
                    homun_id = table.Column<int>(type: "int", nullable: false),
                    elemental_id = table.Column<int>(type: "int", nullable: false),
                    hair = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    hair_color = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    clothes_color = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    body = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    weapon = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    shield = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    head_top = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    head_mid = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    head_bottom = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    robe = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    last_map = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_x = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)53),
                    last_y = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)111),
                    last_instanceid = table.Column<uint>(type: "int unsigned", nullable: false),
                    save_map = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    save_x = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)53),
                    save_y = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)111),
                    partner_id = table.Column<int>(type: "int", nullable: false),
                    online = table.Column<short>(type: "smallint", nullable: false),
                    father = table.Column<int>(type: "int", nullable: false),
                    mother = table.Column<int>(type: "int", nullable: false),
                    child = table.Column<int>(type: "int", nullable: false),
                    fame = table.Column<uint>(type: "int unsigned", nullable: false),
                    rename = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    delete_date = table.Column<uint>(type: "int unsigned", nullable: false),
                    moves = table.Column<uint>(type: "int unsigned", nullable: false),
                    unban_time = table.Column<uint>(type: "int unsigned", nullable: false),
                    font = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    uniqueitem_counter = table.Column<uint>(type: "int unsigned", nullable: false),
                    sex = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hotkey_rowshift = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    hotkey_rowshift2 = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    clan_id = table.Column<int>(type: "int", nullable: false),
                    last_login = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    title_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    show_equip = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    inventory_slots = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)100),
                    body_direction = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    disable_call = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    disable_partyinvite = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    disable_showcostumes = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_char", x => x.char_id);
                    table.ForeignKey(
                        name: "FK_char_clan_clan_id",
                        column: x => x.clan_id,
                        principalTable: "clan",
                        principalColumn: "clan_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_char_guild_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guild",
                        principalColumn: "guild_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_char_login_account_id",
                        column: x => x.account_id,
                        principalTable: "login",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_char_party_party_id",
                        column: x => x.party_id,
                        principalTable: "party",
                        principalColumn: "party_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "vending_items",
                columns: table => new
                {
                    vending_id = table.Column<int>(type: "int", nullable: false),
                    index = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    cartinventory_id = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    price = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vending_items", x => new { x.vending_id, x.index });
                    table.ForeignKey(
                        name: "FK_vending_items_vendings_vending_id",
                        column: x => x.vending_id,
                        principalTable: "vendings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "achievement",
                columns: table => new
                {
                    char_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    id = table.Column<long>(type: "bigint", nullable: false),
                    count1 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    count2 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    count3 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    count4 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    count5 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    count6 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    count7 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    count8 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    count9 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    count10 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    completed = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    rewarded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievement", x => new { x.char_id, x.id });
                    table.ForeignKey(
                        name: "FK_achievement_char_char_id",
                        column: x => x.char_id,
                        principalTable: "char",
                        principalColumn: "char_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "cart_inventory",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    char_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    nameid = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    amount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    equip = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    identify = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    refine = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    attribute = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    card0 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card1 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card2 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card3 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    option_id0 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val0 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm0 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id1 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val1 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm1 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id2 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val2 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm2 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id3 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val3 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm3 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id4 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val4 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm4 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    expire_time = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    bound = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    unique_id = table.Column<ulong>(type: "bigint unsigned", nullable: false, defaultValue: 0ul),
                    enchantgrade = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    CharacterCharId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cart_inventory", x => x.id);
                    table.ForeignKey(
                        name: "FK_cart_inventory_char_CharacterCharId",
                        column: x => x.CharacterCharId,
                        principalTable: "char",
                        principalColumn: "char_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "friends",
                columns: table => new
                {
                    char_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    friend_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CharacterCharId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_friends", x => new { x.char_id, x.friend_id });
                    table.ForeignKey(
                        name: "FK_friends_char_CharacterCharId",
                        column: x => x.CharacterCharId,
                        principalTable: "char",
                        principalColumn: "char_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "hotkey",
                columns: table => new
                {
                    char_id = table.Column<int>(type: "int", nullable: false),
                    hotkey = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    type = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    itemskill_id = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    skill_lvl = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    CharacterCharId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotkey", x => new { x.char_id, x.hotkey });
                    table.ForeignKey(
                        name: "FK_hotkey_char_CharacterCharId",
                        column: x => x.CharacterCharId,
                        principalTable: "char",
                        principalColumn: "char_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "inventory",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    char_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    nameid = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    amount = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    equip = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    identify = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    refine = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    attribute = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    card0 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card1 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card2 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    card3 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    option_id0 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val0 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm0 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id1 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val1 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm1 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id2 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val2 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm2 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id3 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val3 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm3 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    option_id4 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_val4 = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    option_parm4 = table.Column<sbyte>(type: "tinyint", nullable: false, defaultValue: (sbyte)0),
                    expire_time = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    favorite = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    bound = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    unique_id = table.Column<ulong>(type: "bigint unsigned", nullable: false, defaultValue: 0ul),
                    equip_switch = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    enchantgrade = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    CharacterCharId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory", x => x.id);
                    table.ForeignKey(
                        name: "FK_inventory_char_CharacterCharId",
                        column: x => x.CharacterCharId,
                        principalTable: "char",
                        principalColumn: "char_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "memo",
                columns: table => new
                {
                    memo_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    char_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    map = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    x = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    y = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    CharacterCharId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_memo", x => x.memo_id);
                    table.ForeignKey(
                        name: "FK_memo_char_CharacterCharId",
                        column: x => x.CharacterCharId,
                        principalTable: "char",
                        principalColumn: "char_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "quest",
                columns: table => new
                {
                    char_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    quest_id = table.Column<uint>(type: "int unsigned", nullable: false),
                    state = table.Column<string>(type: "longtext", nullable: false, defaultValue: "0")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    time = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    count1 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    count2 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    count3 = table.Column<uint>(type: "int unsigned", nullable: false, defaultValue: 0u),
                    CharacterCharId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quest", x => new { x.char_id, x.quest_id });
                    table.ForeignKey(
                        name: "FK_quest_char_CharacterCharId",
                        column: x => x.CharacterCharId,
                        principalTable: "char",
                        principalColumn: "char_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "skill",
                columns: table => new
                {
                    char_id = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    id = table.Column<ushort>(type: "smallint unsigned", nullable: false, defaultValue: (ushort)0),
                    lv = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0),
                    flag = table.Column<byte>(type: "tinyint unsigned", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill", x => new { x.char_id, x.id });
                    table.ForeignKey(
                        name: "FK_skill_char_char_id",
                        column: x => x.char_id,
                        principalTable: "char",
                        principalColumn: "char_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "account_id",
                table: "acc_reg_num",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "account_id",
                table: "acc_reg_str",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "char_id",
                table: "achievement",
                column: "char_id");

            migrationBuilder.CreateIndex(
                name: "IX_atcommandlog_account_id",
                table: "atcommandlog",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_atcommandlog_char_id",
                table: "atcommandlog",
                column: "char_id");

            migrationBuilder.CreateIndex(
                name: "char_id",
                table: "bonus_script",
                column: "char_id");

            migrationBuilder.CreateIndex(
                name: "IX_branchlog_account_id",
                table: "branchlog",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_branchlog_char_id",
                table: "branchlog",
                column: "char_id");

            migrationBuilder.CreateIndex(
                name: "char_id",
                table: "cart_inventory",
                column: "char_id");

            migrationBuilder.CreateIndex(
                name: "IX_cart_inventory_CharacterCharId",
                table: "cart_inventory",
                column: "CharacterCharId");

            migrationBuilder.CreateIndex(
                name: "type",
                table: "cashlog",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "account_id",
                table: "char",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "guild_id",
                table: "char",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "IX_char_clan_id",
                table: "char",
                column: "clan_id");

            migrationBuilder.CreateIndex(
                name: "name_key",
                table: "char",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "online",
                table: "char",
                column: "online");

            migrationBuilder.CreateIndex(
                name: "party_id",
                table: "char",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "char_id",
                table: "char_reg_num",
                column: "char_id");

            migrationBuilder.CreateIndex(
                name: "char_id",
                table: "char_reg_str",
                column: "char_id");

            migrationBuilder.CreateIndex(
                name: "account_id",
                table: "charlog",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_chatlog_src_accountid",
                table: "chatlog",
                column: "src_accountid");

            migrationBuilder.CreateIndex(
                name: "IX_chatlog_src_charid",
                table: "chatlog",
                column: "src_charid");

            migrationBuilder.CreateIndex(
                name: "alliance_id",
                table: "clan_alliance",
                column: "alliance_id");

            migrationBuilder.CreateIndex(
                name: "IX_friends_CharacterCharId",
                table: "friends",
                column: "CharacterCharId");

            migrationBuilder.CreateIndex(
                name: "account_id",
                table: "global_acc_reg_num",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "account_id",
                table: "global_acc_reg_str",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "char_id",
                table: "guild",
                column: "char_id");

            migrationBuilder.CreateIndex(
                name: "alliance_id",
                table: "guild_alliance",
                column: "alliance_id");

            migrationBuilder.CreateIndex(
                name: "guild_id",
                table: "guild_castle",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "char_id",
                table: "guild_member",
                column: "char_id");

            migrationBuilder.CreateIndex(
                name: "guild_id",
                table: "guild_storage",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "IX_guild_storage_log_guild_id",
                table: "guild_storage_log",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "IX_hotkey_CharacterCharId",
                table: "hotkey",
                column: "CharacterCharId");

            migrationBuilder.CreateIndex(
                name: "time",
                table: "interlog",
                column: "time");

            migrationBuilder.CreateIndex(
                name: "char_id",
                table: "inventory",
                column: "char_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_CharacterCharId",
                table: "inventory",
                column: "CharacterCharId");

            migrationBuilder.CreateIndex(
                name: "UniqueAegisName",
                table: "item_db",
                column: "name_aegis",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "name",
                table: "login",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "web_auth_token_key",
                table: "login",
                column: "web_auth_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_loginlog_ip",
                table: "loginlog",
                column: "ip");

            migrationBuilder.CreateIndex(
                name: "IX_mail_attachments_MailId",
                table: "mail_attachments",
                column: "MailId");

            migrationBuilder.CreateIndex(
                name: "char_id",
                table: "memo",
                column: "char_id");

            migrationBuilder.CreateIndex(
                name: "IX_memo_CharacterCharId",
                table: "memo",
                column: "CharacterCharId");

            migrationBuilder.CreateIndex(
                name: "IX_mob_db_name_aegis",
                table: "mob_db",
                column: "name_aegis",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_npclog_account_id",
                table: "npclog",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_npclog_char_id",
                table: "npclog",
                column: "char_id");

            migrationBuilder.CreateIndex(
                name: "type",
                table: "picklog",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IX_quest_CharacterCharId",
                table: "quest",
                column: "CharacterCharId");

            migrationBuilder.CreateIndex(
                name: "account_id",
                table: "storage",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "type",
                table: "zenylog",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "acc_reg_num");

            migrationBuilder.DropTable(
                name: "acc_reg_str");

            migrationBuilder.DropTable(
                name: "achievement");

            migrationBuilder.DropTable(
                name: "atcommandlog");

            migrationBuilder.DropTable(
                name: "auction");

            migrationBuilder.DropTable(
                name: "barter");

            migrationBuilder.DropTable(
                name: "bonus_script");

            migrationBuilder.DropTable(
                name: "branchlog");

            migrationBuilder.DropTable(
                name: "buyingstore_items");

            migrationBuilder.DropTable(
                name: "cart_inventory");

            migrationBuilder.DropTable(
                name: "cashlog");

            migrationBuilder.DropTable(
                name: "char_configs");

            migrationBuilder.DropTable(
                name: "char_reg_num");

            migrationBuilder.DropTable(
                name: "char_reg_str");

            migrationBuilder.DropTable(
                name: "charlog");

            migrationBuilder.DropTable(
                name: "chatlog");

            migrationBuilder.DropTable(
                name: "clan_alliance");

            migrationBuilder.DropTable(
                name: "db_roulette");

            migrationBuilder.DropTable(
                name: "elemental");

            migrationBuilder.DropTable(
                name: "feedinglog");

            migrationBuilder.DropTable(
                name: "friends");

            migrationBuilder.DropTable(
                name: "global_acc_reg_num");

            migrationBuilder.DropTable(
                name: "global_acc_reg_str");

            migrationBuilder.DropTable(
                name: "guild_alliance");

            migrationBuilder.DropTable(
                name: "guild_castle");

            migrationBuilder.DropTable(
                name: "guild_emblems");

            migrationBuilder.DropTable(
                name: "guild_expulsion");

            migrationBuilder.DropTable(
                name: "guild_member");

            migrationBuilder.DropTable(
                name: "guild_position");

            migrationBuilder.DropTable(
                name: "guild_skill");

            migrationBuilder.DropTable(
                name: "guild_storage");

            migrationBuilder.DropTable(
                name: "guild_storage_log");

            migrationBuilder.DropTable(
                name: "hotkey");

            migrationBuilder.DropTable(
                name: "interlog");

            migrationBuilder.DropTable(
                name: "inventory");

            migrationBuilder.DropTable(
                name: "ipbanlist");

            migrationBuilder.DropTable(
                name: "item_db");

            migrationBuilder.DropTable(
                name: "loginlog");

            migrationBuilder.DropTable(
                name: "mail_attachments");

            migrationBuilder.DropTable(
                name: "mapreg");

            migrationBuilder.DropTable(
                name: "market");

            migrationBuilder.DropTable(
                name: "memo");

            migrationBuilder.DropTable(
                name: "mercenary");

            migrationBuilder.DropTable(
                name: "mercenary_owner");

            migrationBuilder.DropTable(
                name: "merchant_configs");

            migrationBuilder.DropTable(
                name: "mob_db");

            migrationBuilder.DropTable(
                name: "mvplog");

            migrationBuilder.DropTable(
                name: "npclog");

            migrationBuilder.DropTable(
                name: "party_bookings");

            migrationBuilder.DropTable(
                name: "pet");

            migrationBuilder.DropTable(
                name: "picklog");

            migrationBuilder.DropTable(
                name: "quest");

            migrationBuilder.DropTable(
                name: "sales");

            migrationBuilder.DropTable(
                name: "sc_data");

            migrationBuilder.DropTable(
                name: "skill");

            migrationBuilder.DropTable(
                name: "skill_homunculus");

            migrationBuilder.DropTable(
                name: "skillcooldown");

            migrationBuilder.DropTable(
                name: "skillcooldown_homunculus");

            migrationBuilder.DropTable(
                name: "skillcooldown_mercenary");

            migrationBuilder.DropTable(
                name: "storage");

            migrationBuilder.DropTable(
                name: "user_configs");

            migrationBuilder.DropTable(
                name: "vending_items");

            migrationBuilder.DropTable(
                name: "zenylog");

            migrationBuilder.DropTable(
                name: "buyingstores");

            migrationBuilder.DropTable(
                name: "mail");

            migrationBuilder.DropTable(
                name: "char");

            migrationBuilder.DropTable(
                name: "homunculus");

            migrationBuilder.DropTable(
                name: "vendings");

            migrationBuilder.DropTable(
                name: "clan");

            migrationBuilder.DropTable(
                name: "guild");

            migrationBuilder.DropTable(
                name: "login");

            migrationBuilder.DropTable(
                name: "party");
        }
    }
}
