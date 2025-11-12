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
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Index = table.Column<uint>(type: "int unsigned", nullable: false),
                    Value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acc_reg_num", x => new { x.AccountId, x.Key, x.Index });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "acc_reg_str",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Index = table.Column<uint>(type: "int unsigned", nullable: false),
                    Value = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acc_reg_str", x => new { x.AccountId, x.Key, x.Index });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "atcommandlog",
                columns: table => new
                {
                    AtCommandId = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AtCommandDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    CharName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Map = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Command = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_atcommandlog", x => x.AtCommandId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "auction",
                columns: table => new
                {
                    AuctionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SellerId = table.Column<int>(type: "int", nullable: false),
                    SellerName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BuyerId = table.Column<int>(type: "int", nullable: false),
                    BuyerName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Price = table.Column<uint>(type: "int unsigned", nullable: false),
                    Buynow = table.Column<uint>(type: "int unsigned", nullable: false),
                    Hours = table.Column<short>(type: "smallint", nullable: false),
                    Timestamp = table.Column<uint>(type: "int unsigned", nullable: false),
                    NameId = table.Column<uint>(type: "int unsigned", nullable: false),
                    ItemName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<short>(type: "smallint", nullable: false),
                    Refine = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Attribute = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Card0 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card1 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card2 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card3 = table.Column<uint>(type: "int unsigned", nullable: false),
                    OptionId0 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal0 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm0 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId1 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal1 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm1 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId2 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal2 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm2 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId3 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal3 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm3 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId4 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal4 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm4 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    UniqueId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    EnchantGrade = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auction", x => x.AuctionId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "barter",
                columns: table => new
                {
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Index = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Amount = table.Column<ushort>(type: "smallint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_barter", x => new { x.Name, x.Index });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "branchlog",
                columns: table => new
                {
                    BranchId = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BranchDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    CharName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Map = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branchlog", x => x.BranchId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "buyingstores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Sex = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Map = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    X = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Y = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Limit = table.Column<uint>(type: "int unsigned", nullable: false),
                    BodyDirection = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HeadDirection = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Sit = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Autotrade = table.Column<sbyte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buyingstores", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "cashlog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CashType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Map = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cashlog", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "char_configs",
                columns: table => new
                {
                    WorldName = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_char_configs", x => new { x.WorldName, x.AccountId, x.CharId });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "char_reg_num",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Index = table.Column<uint>(type: "int unsigned", nullable: false),
                    Value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_char_reg_num", x => new { x.CharId, x.Key, x.Index });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "char_reg_str",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Index = table.Column<uint>(type: "int unsigned", nullable: false),
                    Value = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_char_reg_str", x => new { x.CharId, x.Key, x.Index });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "charlog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CharMsg = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CharNum = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Str = table.Column<uint>(type: "int unsigned", nullable: false),
                    Agi = table.Column<uint>(type: "int unsigned", nullable: false),
                    Vit = table.Column<uint>(type: "int unsigned", nullable: false),
                    Int = table.Column<uint>(type: "int unsigned", nullable: false),
                    Dex = table.Column<uint>(type: "int unsigned", nullable: false),
                    Luk = table.Column<uint>(type: "int unsigned", nullable: false),
                    Hair = table.Column<sbyte>(type: "tinyint", nullable: false),
                    HairColor = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_charlog", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "chatlog",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Type = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TypeId = table.Column<int>(type: "int", nullable: false),
                    SrcCharId = table.Column<int>(type: "int", nullable: false),
                    SrcAccountId = table.Column<int>(type: "int", nullable: false),
                    SrcMap = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SrcMapX = table.Column<short>(type: "smallint", nullable: false),
                    SrcMapY = table.Column<short>(type: "smallint", nullable: false),
                    DstCharName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Message = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chatlog", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "clan",
                columns: table => new
                {
                    clan_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    master = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    mapname = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    max_member = table.Column<ushort>(type: "smallint unsigned", nullable: false)
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
                    Index = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Level = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    ItemId = table.Column<uint>(type: "int unsigned", nullable: false),
                    Amount = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Flag = table.Column<ushort>(type: "smallint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_db_roulette", x => x.Index);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "elemental",
                columns: table => new
                {
                    EleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Class = table.Column<uint>(type: "int unsigned", nullable: false),
                    Mode = table.Column<uint>(type: "int unsigned", nullable: false),
                    Hp = table.Column<uint>(type: "int unsigned", nullable: false),
                    Sp = table.Column<uint>(type: "int unsigned", nullable: false),
                    MaxHp = table.Column<uint>(type: "int unsigned", nullable: false),
                    MaxSp = table.Column<uint>(type: "int unsigned", nullable: false),
                    Atk1 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Atk2 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Matk = table.Column<uint>(type: "int unsigned", nullable: false),
                    Aspd = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Def = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Mdef = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Flee = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Hit = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    LifeTime = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_elemental", x => x.EleId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "feedinglog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    TargetId = table.Column<int>(type: "int", nullable: false),
                    TargetClass = table.Column<short>(type: "smallint", nullable: false),
                    Type = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Intimacy = table.Column<uint>(type: "int unsigned", nullable: false),
                    ItemId = table.Column<uint>(type: "int unsigned", nullable: false),
                    Map = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    X = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Y = table.Column<ushort>(type: "smallint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feedinglog", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "global_acc_reg_num",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Index = table.Column<uint>(type: "int unsigned", nullable: false),
                    Value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_global_acc_reg_num", x => new { x.AccountId, x.Key, x.Index });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "global_acc_reg_str",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Index = table.Column<uint>(type: "int unsigned", nullable: false),
                    Value = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_global_acc_reg_str", x => new { x.AccountId, x.Key, x.Index });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "guild",
                columns: table => new
                {
                    GuildId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Master = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GuildLv = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    ConnectMember = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    MaxMember = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    AverageLv = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Exp = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    NextExp = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    SkillPoint = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Mes1 = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Mes2 = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmblemLen = table.Column<uint>(type: "int unsigned", nullable: false),
                    EmblemId = table.Column<uint>(type: "int unsigned", nullable: false),
                    EmblemData = table.Column<byte[]>(type: "longblob", nullable: true),
                    LastMasterChange = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild", x => x.GuildId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "guild_castle",
                columns: table => new
                {
                    CastleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GuildId = table.Column<int>(type: "int", nullable: false),
                    Economy = table.Column<uint>(type: "int unsigned", nullable: false),
                    Defense = table.Column<uint>(type: "int unsigned", nullable: false),
                    TriggerE = table.Column<uint>(type: "int unsigned", nullable: false),
                    TriggerD = table.Column<uint>(type: "int unsigned", nullable: false),
                    NextTime = table.Column<uint>(type: "int unsigned", nullable: false),
                    PayTime = table.Column<uint>(type: "int unsigned", nullable: false),
                    CreateTime = table.Column<uint>(type: "int unsigned", nullable: false),
                    VisibleC = table.Column<uint>(type: "int unsigned", nullable: false),
                    VisibleG0 = table.Column<uint>(type: "int unsigned", nullable: false),
                    VisibleG1 = table.Column<uint>(type: "int unsigned", nullable: false),
                    VisibleG2 = table.Column<uint>(type: "int unsigned", nullable: false),
                    VisibleG3 = table.Column<uint>(type: "int unsigned", nullable: false),
                    VisibleG4 = table.Column<uint>(type: "int unsigned", nullable: false),
                    VisibleG5 = table.Column<uint>(type: "int unsigned", nullable: false),
                    VisibleG6 = table.Column<uint>(type: "int unsigned", nullable: false),
                    VisibleG7 = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_castle", x => x.CastleId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "guild_emblems",
                columns: table => new
                {
                    WorldName = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GuildId = table.Column<int>(type: "int", nullable: false),
                    FileType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileData = table.Column<byte[]>(type: "longblob", nullable: true),
                    Version = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_emblems", x => new { x.WorldName, x.GuildId });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "guild_expulsion",
                columns: table => new
                {
                    GuildId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Mes = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CharId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_expulsion", x => new { x.GuildId, x.Name });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "guild_storage_log",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GuildId = table.Column<int>(type: "int", nullable: false),
                    Time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NameId = table.Column<uint>(type: "int unsigned", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Identify = table.Column<short>(type: "smallint", nullable: false),
                    Refine = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Attribute = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Card0 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card1 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card2 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card3 = table.Column<uint>(type: "int unsigned", nullable: false),
                    OptionId0 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal0 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm0 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId1 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal1 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm1 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId2 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal2 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm2 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId3 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal3 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm3 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId4 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal4 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm4 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    ExpireTime = table.Column<uint>(type: "int unsigned", nullable: false),
                    UniqueId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Bound = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    EnchantGrade = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_storage_log", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "homunculus",
                columns: table => new
                {
                    HomunId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Class = table.Column<uint>(type: "int unsigned", nullable: false),
                    PrevClass = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Level = table.Column<short>(type: "smallint", nullable: false),
                    Exp = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Intimacy = table.Column<int>(type: "int", nullable: false),
                    Hunger = table.Column<short>(type: "smallint", nullable: false),
                    Str = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Agi = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Vit = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Int = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Dex = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Luk = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Hp = table.Column<uint>(type: "int unsigned", nullable: false),
                    MaxHp = table.Column<uint>(type: "int unsigned", nullable: false),
                    Sp = table.Column<uint>(type: "int unsigned", nullable: false),
                    MaxSp = table.Column<uint>(type: "int unsigned", nullable: false),
                    SkillPoint = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Alive = table.Column<short>(type: "smallint", nullable: false),
                    RenameFlag = table.Column<short>(type: "smallint", nullable: false),
                    Vaporize = table.Column<short>(type: "smallint", nullable: false),
                    Autofeed = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_homunculus", x => x.HomunId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "interlog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Log = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interlog", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ipbanlist",
                columns: table => new
                {
                    List = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Reason = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ipbanlist", x => new { x.List, x.BTime });
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
                    group_id = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    state = table.Column<uint>(type: "int unsigned", nullable: false),
                    unban_time = table.Column<uint>(type: "int unsigned", nullable: false),
                    expiration_time = table.Column<uint>(type: "int unsigned", nullable: false),
                    logincount = table.Column<uint>(type: "int unsigned", nullable: false),
                    lastlogin = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_ip = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    birthdate = table.Column<DateOnly>(type: "date", nullable: true),
                    character_slots = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    pincode = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pincode_change = table.Column<uint>(type: "int unsigned", nullable: false),
                    vip_time = table.Column<uint>(type: "int unsigned", nullable: false),
                    old_group = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    web_auth_token = table.Column<string>(type: "varchar(17)", maxLength: 17, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    web_auth_token_enabled = table.Column<short>(type: "smallint", nullable: false)
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
                    Time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Ip = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    User = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RCode = table.Column<sbyte>(type: "tinyint", nullable: false),
                    Log = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loginlog", x => new { x.Time, x.Ip, x.User });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mail",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SendName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SendId = table.Column<int>(type: "int", nullable: false),
                    DestName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DestId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Message = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Time = table.Column<uint>(type: "int unsigned", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    Zeny = table.Column<uint>(type: "int unsigned", nullable: false),
                    Type = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mail", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mapreg",
                columns: table => new
                {
                    VarName = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Index = table.Column<uint>(type: "int unsigned", nullable: false),
                    Value = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mapreg", x => new { x.VarName, x.Index });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "market",
                columns: table => new
                {
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NameId = table.Column<uint>(type: "int unsigned", nullable: false),
                    Price = table.Column<uint>(type: "int unsigned", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Flag = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_market", x => new { x.Name, x.NameId });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mercenary",
                columns: table => new
                {
                    MerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Class = table.Column<uint>(type: "int unsigned", nullable: false),
                    Hp = table.Column<uint>(type: "int unsigned", nullable: false),
                    Sp = table.Column<uint>(type: "int unsigned", nullable: false),
                    KillCounter = table.Column<int>(type: "int", nullable: false),
                    LifeTime = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mercenary", x => x.MerId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mercenary_owner",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MercId = table.Column<int>(type: "int", nullable: false),
                    ArchCalls = table.Column<int>(type: "int", nullable: false),
                    ArchFaith = table.Column<int>(type: "int", nullable: false),
                    SpearCalls = table.Column<int>(type: "int", nullable: false),
                    SpearFaith = table.Column<int>(type: "int", nullable: false),
                    SwordCalls = table.Column<int>(type: "int", nullable: false),
                    SwordFaith = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mercenary_owner", x => x.CharId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "merchant_configs",
                columns: table => new
                {
                    WorldName = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    StoreType = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Data = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_merchant_configs", x => new { x.WorldName, x.AccountId, x.CharId, x.StoreType });
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
                    MvpId = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MvpDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    KillCharId = table.Column<int>(type: "int", nullable: false),
                    MonsterId = table.Column<short>(type: "smallint", nullable: false),
                    Prize = table.Column<uint>(type: "int unsigned", nullable: false),
                    MvpExp = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Map = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mvplog", x => x.MvpId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "npclog",
                columns: table => new
                {
                    NpcId = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NpcDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    CharName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Map = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Mes = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npclog", x => x.NpcId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "party",
                columns: table => new
                {
                    PartyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Exp = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Item = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    LeaderId = table.Column<int>(type: "int", nullable: false),
                    LeaderChar = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_party", x => x.PartyId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "party_bookings",
                columns: table => new
                {
                    WorldName = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    CharName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Purpose = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Assist = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    DamageDealer = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Healer = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Tanker = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    MinimumLevel = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    MaximumLevel = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Comment = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Created = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_party_bookings", x => new { x.WorldName, x.AccountId, x.CharId });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pet",
                columns: table => new
                {
                    PetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Class = table.Column<uint>(type: "int unsigned", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    EggId = table.Column<uint>(type: "int unsigned", nullable: false),
                    Equip = table.Column<uint>(type: "int unsigned", nullable: false),
                    Intimate = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Hungry = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    RenameFlag = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Incubate = table.Column<uint>(type: "int unsigned", nullable: false),
                    Autofeed = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pet", x => x.PetId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "picklog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NameId = table.Column<uint>(type: "int unsigned", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Refine = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Card0 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card1 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card2 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card3 = table.Column<uint>(type: "int unsigned", nullable: false),
                    OptionId0 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal0 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm0 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId1 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal1 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm1 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId2 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal2 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm2 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId3 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal3 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm3 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId4 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal4 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm4 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    UniqueId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Map = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Bound = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    EnchantGrade = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_picklog", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sales",
                columns: table => new
                {
                    NameId = table.Column<uint>(type: "int unsigned", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Start = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    End = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales", x => x.NameId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sc_data",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Tick = table.Column<long>(type: "bigint", nullable: false),
                    Val1 = table.Column<int>(type: "int", nullable: false),
                    Val2 = table.Column<int>(type: "int", nullable: false),
                    Val3 = table.Column<int>(type: "int", nullable: false),
                    Val4 = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sc_data", x => new { x.CharId, x.Type });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "skillcooldown",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Skill = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Tick = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skillcooldown", x => new { x.CharId, x.Skill });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "skillcooldown_homunculus",
                columns: table => new
                {
                    HomunId = table.Column<int>(type: "int", nullable: false),
                    Skill = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Tick = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skillcooldown_homunculus", x => new { x.HomunId, x.Skill });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "skillcooldown_mercenary",
                columns: table => new
                {
                    MerId = table.Column<int>(type: "int", nullable: false),
                    Skill = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Tick = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skillcooldown_mercenary", x => new { x.MerId, x.Skill });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "storage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    NameId = table.Column<uint>(type: "int unsigned", nullable: false),
                    Amount = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Equip = table.Column<uint>(type: "int unsigned", nullable: false),
                    Identify = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Refine = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Attribute = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Card0 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card1 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card2 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card3 = table.Column<uint>(type: "int unsigned", nullable: false),
                    OptionId0 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal0 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm0 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId1 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal1 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm1 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId2 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal2 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm2 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId3 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal3 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm3 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId4 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal4 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm4 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    ExpireTime = table.Column<uint>(type: "int unsigned", nullable: false),
                    Bound = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    UniqueId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    EnchantGrade = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_storage", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "user_configs",
                columns: table => new
                {
                    WorldName = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_configs", x => new { x.WorldName, x.AccountId });
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "vendings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AccountId = table.Column<int>(type: "int", nullable: false),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Sex = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Map = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    X = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Y = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Title = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BodyDirection = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HeadDirection = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Sit = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Autotrade = table.Column<sbyte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vendings", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "zenylog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    SrcId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Map = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_zenylog", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "buyingstore_items",
                columns: table => new
                {
                    BuyingStoreId = table.Column<int>(type: "int", nullable: false),
                    Index = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    ItemId = table.Column<uint>(type: "int unsigned", nullable: false),
                    Amount = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Price = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_buyingstore_items", x => new { x.BuyingStoreId, x.Index });
                    table.ForeignKey(
                        name: "FK_buyingstore_items_buyingstores_BuyingStoreId",
                        column: x => x.BuyingStoreId,
                        principalTable: "buyingstores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "clan_alliance",
                columns: table => new
                {
                    clan_id = table.Column<int>(type: "int", nullable: false),
                    opposition = table.Column<int>(type: "int", nullable: false),
                    alliance_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "varchar(24)", maxLength: 24, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clan_alliance", x => new { x.clan_id, x.opposition, x.alliance_id });
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
                    GuildId = table.Column<int>(type: "int", nullable: false),
                    AllianceId = table.Column<int>(type: "int", nullable: false),
                    Opposition = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_alliance", x => new { x.GuildId, x.AllianceId });
                    table.ForeignKey(
                        name: "FK_guild_alliance_guild_GuildId",
                        column: x => x.GuildId,
                        principalTable: "guild",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "guild_member",
                columns: table => new
                {
                    GuildId = table.Column<int>(type: "int", nullable: false),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Exp = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Position = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_member", x => new { x.GuildId, x.CharId });
                    table.ForeignKey(
                        name: "FK_guild_member_guild_GuildId",
                        column: x => x.GuildId,
                        principalTable: "guild",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "guild_position",
                columns: table => new
                {
                    GuildId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Mode = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    ExpMode = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_position", x => new { x.GuildId, x.Position });
                    table.ForeignKey(
                        name: "FK_guild_position_guild_GuildId",
                        column: x => x.GuildId,
                        principalTable: "guild",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "guild_skill",
                columns: table => new
                {
                    GuildId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Lv = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_skill", x => new { x.GuildId, x.Id });
                    table.ForeignKey(
                        name: "FK_guild_skill_guild_GuildId",
                        column: x => x.GuildId,
                        principalTable: "guild",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "guild_storage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GuildId = table.Column<int>(type: "int", nullable: false),
                    NameId = table.Column<uint>(type: "int unsigned", nullable: false),
                    Amount = table.Column<uint>(type: "int unsigned", nullable: false),
                    Equip = table.Column<uint>(type: "int unsigned", nullable: false),
                    Identify = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Refine = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Attribute = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Card0 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card1 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card2 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card3 = table.Column<uint>(type: "int unsigned", nullable: false),
                    OptionId0 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal0 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm0 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId1 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal1 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm1 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId2 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal2 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm2 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId3 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal3 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm3 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId4 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal4 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm4 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    ExpireTime = table.Column<uint>(type: "int unsigned", nullable: false),
                    Bound = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    UniqueId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    EnchantGrade = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guild_storage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_guild_storage_guild_GuildId",
                        column: x => x.GuildId,
                        principalTable: "guild",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "skill_homunculus",
                columns: table => new
                {
                    HomunId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
                    Lv = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_homunculus", x => new { x.HomunId, x.Id });
                    table.ForeignKey(
                        name: "FK_skill_homunculus_homunculus_HomunId",
                        column: x => x.HomunId,
                        principalTable: "homunculus",
                        principalColumn: "HomunId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mail_attachments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Index = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    NameId = table.Column<uint>(type: "int unsigned", nullable: false),
                    Amount = table.Column<uint>(type: "int unsigned", nullable: false),
                    Refine = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Attribute = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Identify = table.Column<short>(type: "smallint", nullable: false),
                    Card0 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card1 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card2 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card3 = table.Column<uint>(type: "int unsigned", nullable: false),
                    OptionId0 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal0 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm0 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId1 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal1 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm1 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId2 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal2 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm2 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId3 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal3 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm3 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId4 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal4 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm4 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    UniqueId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    Bound = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    EnchantGrade = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    MailId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mail_attachments", x => new { x.Id, x.Index });
                    table.ForeignKey(
                        name: "FK_mail_attachments_mail_MailId",
                        column: x => x.MailId,
                        principalTable: "mail",
                        principalColumn: "Id");
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
                        principalColumn: "GuildId",
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
                        principalColumn: "PartyId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "vending_items",
                columns: table => new
                {
                    VendingId = table.Column<int>(type: "int", nullable: false),
                    Index = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    CartInventoryId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Price = table.Column<uint>(type: "int unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vending_items", x => new { x.VendingId, x.Index });
                    table.ForeignKey(
                        name: "FK_vending_items_vendings_VendingId",
                        column: x => x.VendingId,
                        principalTable: "vendings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "achievement",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Count1 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Count2 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Count3 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Count4 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Count5 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Count6 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Count7 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Count8 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Count9 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Count10 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Completed = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Rewarded = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievement", x => new { x.CharId, x.Id });
                    table.ForeignKey(
                        name: "FK_achievement_char_CharId",
                        column: x => x.CharId,
                        principalTable: "char",
                        principalColumn: "char_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "bonus_script",
                columns: table => new
                {
                    CharId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Script = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Tick = table.Column<long>(type: "bigint", nullable: false),
                    Flag = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Icon = table.Column<short>(type: "smallint", nullable: false),
                    CharacterCharId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bonus_script", x => x.CharId);
                    table.ForeignKey(
                        name: "FK_bonus_script_char_CharacterCharId",
                        column: x => x.CharacterCharId,
                        principalTable: "char",
                        principalColumn: "char_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "cart_inventory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    NameId = table.Column<uint>(type: "int unsigned", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Equip = table.Column<uint>(type: "int unsigned", nullable: false),
                    Identify = table.Column<short>(type: "smallint", nullable: false),
                    Refine = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Attribute = table.Column<sbyte>(type: "tinyint", nullable: false),
                    Card0 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card1 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card2 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card3 = table.Column<uint>(type: "int unsigned", nullable: false),
                    OptionId0 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal0 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm0 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId1 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal1 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm1 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId2 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal2 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm2 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId3 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal3 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm3 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId4 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal4 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm4 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    ExpireTime = table.Column<uint>(type: "int unsigned", nullable: false),
                    Bound = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    UniqueId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    EnchantGrade = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    CharacterCharId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cart_inventory", x => x.Id);
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
                    CharId = table.Column<int>(type: "int", nullable: false),
                    FriendId = table.Column<int>(type: "int", nullable: false),
                    CharacterCharId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_friends", x => new { x.CharId, x.FriendId });
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
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Hotkey = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    ItemSkillId = table.Column<uint>(type: "int unsigned", nullable: false),
                    SkillLvl = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    CharacterCharId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hotkey", x => new { x.CharId, x.Hotkey });
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
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    NameId = table.Column<uint>(type: "int unsigned", nullable: false),
                    Amount = table.Column<uint>(type: "int unsigned", nullable: false),
                    Equip = table.Column<uint>(type: "int unsigned", nullable: false),
                    Identify = table.Column<short>(type: "smallint", nullable: false),
                    Refine = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Attribute = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Card0 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card1 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card2 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Card3 = table.Column<uint>(type: "int unsigned", nullable: false),
                    OptionId0 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal0 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm0 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId1 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal1 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm1 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId2 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal2 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm2 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId3 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal3 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm3 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    OptionId4 = table.Column<short>(type: "smallint", nullable: false),
                    OptionVal4 = table.Column<short>(type: "smallint", nullable: false),
                    OptionParm4 = table.Column<sbyte>(type: "tinyint", nullable: false),
                    ExpireTime = table.Column<uint>(type: "int unsigned", nullable: false),
                    Favorite = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Bound = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    UniqueId = table.Column<ulong>(type: "bigint unsigned", nullable: false),
                    EquipSwitch = table.Column<uint>(type: "int unsigned", nullable: false),
                    EnchantGrade = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    CharacterCharId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory", x => x.Id);
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
                    MemoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Map = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    X = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Y = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    CharacterCharId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_memo", x => x.MemoId);
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
                    CharId = table.Column<int>(type: "int", nullable: false),
                    QuestId = table.Column<uint>(type: "int unsigned", nullable: false),
                    State = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Time = table.Column<uint>(type: "int unsigned", nullable: false),
                    Count1 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Count2 = table.Column<uint>(type: "int unsigned", nullable: false),
                    Count3 = table.Column<uint>(type: "int unsigned", nullable: false),
                    CharacterCharId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quest", x => new { x.CharId, x.QuestId });
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
                    CharId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<ushort>(type: "smallint unsigned", nullable: false),
                    Lv = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Flag = table.Column<byte>(type: "tinyint unsigned", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill", x => new { x.CharId, x.Id });
                    table.ForeignKey(
                        name: "FK_skill_char_CharId",
                        column: x => x.CharId,
                        principalTable: "char",
                        principalColumn: "char_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_bonus_script_CharacterCharId",
                table: "bonus_script",
                column: "CharacterCharId");

            migrationBuilder.CreateIndex(
                name: "IX_cart_inventory_CharacterCharId",
                table: "cart_inventory",
                column: "CharacterCharId");

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
                name: "alliance_id",
                table: "clan_alliance",
                column: "alliance_id");

            migrationBuilder.CreateIndex(
                name: "IX_friends_CharacterCharId",
                table: "friends",
                column: "CharacterCharId");

            migrationBuilder.CreateIndex(
                name: "IX_guild_storage_GuildId",
                table: "guild_storage",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_hotkey_CharacterCharId",
                table: "hotkey",
                column: "CharacterCharId");

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
                name: "IX_mail_attachments_MailId",
                table: "mail_attachments",
                column: "MailId");

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
                name: "IX_quest_CharacterCharId",
                table: "quest",
                column: "CharacterCharId");
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
