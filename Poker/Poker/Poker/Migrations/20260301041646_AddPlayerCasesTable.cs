using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Poker.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerCasesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Card",
                columns: table => new
                {
                    card = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Card_pk", x => x.card);
                });

            migrationBuilder.CreateTable(
                name: "CardReverseSkin",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    filename = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("CardReverseSkin_pk", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Player",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    email = table.Column<string>(type: "TEXT", nullable: false),
                    password = table.Column<string>(type: "TEXT", nullable: false),
                    balance = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Player_pk", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Table",
                columns: table => new
                {
                    join_code = table.Column<string>(type: "TEXT", nullable: false),
                    buy_in = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("Table_pk", x => x.join_code);
                });

            migrationBuilder.CreateTable(
                name: "CardFrontSkin",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    filename = table.Column<string>(type: "TEXT", nullable: false),
                    card = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("CardFrontSkin_pk", x => x.id);
                    table.ForeignKey(
                        name: "CardFrontSkin_Card",
                        column: x => x.card,
                        principalTable: "Card",
                        principalColumn: "card",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerCases",
                columns: table => new
                {
                    Player_id = table.Column<int>(type: "INTEGER", nullable: false),
                    number = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PlayerCase_pk", x => x.Player_id);
                    table.ForeignKey(
                        name: "FK_PlayerCases_Player_Player_id",
                        column: x => x.Player_id,
                        principalTable: "Player",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerOwnedReverseSkin",
                columns: table => new
                {
                    Player_id = table.Column<int>(type: "INTEGER", nullable: false),
                    Skin_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PlayerOwnedReverseSkin_pk", x => new { x.Player_id, x.Skin_id });
                    table.ForeignKey(
                        name: "FK_PlayerOwnedReverseSkin_CardReverseSkin_Skin_id",
                        column: x => x.Skin_id,
                        principalTable: "CardReverseSkin",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerOwnedReverseSkin_Player_Player_id",
                        column: x => x.Player_id,
                        principalTable: "Player",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                columns: table => new
                {
                    id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Player_id = table.Column<int>(type: "INTEGER", nullable: false),
                    token_hash = table.Column<string>(type: "TEXT", nullable: false),
                    expires_at = table.Column<DateTime>(type: "TEXT", nullable: false),
                    revoked = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("RefreshToken_pk", x => x.id);
                    table.ForeignKey(
                        name: "FK_RefreshToken_Player_Player_id",
                        column: x => x.Player_id,
                        principalTable: "Player",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerTable",
                columns: table => new
                {
                    Player_id = table.Column<int>(type: "INTEGER", nullable: false),
                    Table_joinCode = table.Column<string>(type: "TEXT", nullable: false),
                    table_balance = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PlayerTable_pk", x => x.Player_id);
                    table.ForeignKey(
                        name: "FK_PlayerTable_Player_Player_id",
                        column: x => x.Player_id,
                        principalTable: "Player",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerTable_Table_Table_joinCode",
                        column: x => x.Table_joinCode,
                        principalTable: "Table",
                        principalColumn: "join_code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerOwnedFaceSkin",
                columns: table => new
                {
                    Player_id = table.Column<int>(type: "INTEGER", nullable: false),
                    Skin_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PlayerOwnedFaceSkin_pk", x => new { x.Player_id, x.Skin_id });
                    table.ForeignKey(
                        name: "FK_PlayerOwnedFaceSkin_CardFrontSkin_Skin_id",
                        column: x => x.Skin_id,
                        principalTable: "CardFrontSkin",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerOwnedFaceSkin_Player_Player_id",
                        column: x => x.Player_id,
                        principalTable: "Player",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerEquippedReverseSkin",
                columns: table => new
                {
                    Player_id = table.Column<int>(type: "INTEGER", nullable: false),
                    Skin_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PlayerEquippedReverseSkin_pk", x => x.Player_id);
                    table.ForeignKey(
                        name: "FK_PlayerEquippedReverseSkin_PlayerOwnedReverseSkin_Player_id_Skin_id",
                        columns: x => new { x.Player_id, x.Skin_id },
                        principalTable: "PlayerOwnedReverseSkin",
                        principalColumns: new[] { "Player_id", "Skin_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerEquippedFaceSkin",
                columns: table => new
                {
                    Player_id = table.Column<int>(type: "INTEGER", nullable: false),
                    card = table.Column<string>(type: "TEXT", nullable: false),
                    Skin_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PlayerEquippedFaceSkin_pk", x => new { x.Player_id, x.card });
                    table.ForeignKey(
                        name: "PlayerEquippedFaceSkin_Card",
                        column: x => x.card,
                        principalTable: "Card",
                        principalColumn: "card",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "PlayerEquippedFaceSkin_PlayerOwnedFaceSkin",
                        columns: x => new { x.Player_id, x.Skin_id },
                        principalTable: "PlayerOwnedFaceSkin",
                        principalColumns: new[] { "Player_id", "Skin_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardFrontSkin_card",
                table: "CardFrontSkin",
                column: "card");

            migrationBuilder.CreateIndex(
                name: "UQ_Player_Email",
                table: "Player",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Player_Name",
                table: "Player",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerEquippedFaceSkin_card",
                table: "PlayerEquippedFaceSkin",
                column: "card");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerEquippedFaceSkin_Player_id_Skin_id",
                table: "PlayerEquippedFaceSkin",
                columns: new[] { "Player_id", "Skin_id" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerEquippedReverseSkin_Player_id_Skin_id",
                table: "PlayerEquippedReverseSkin",
                columns: new[] { "Player_id", "Skin_id" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerOwnedFaceSkin_Skin_id",
                table: "PlayerOwnedFaceSkin",
                column: "Skin_id");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerOwnedReverseSkin_Skin_id",
                table: "PlayerOwnedReverseSkin",
                column: "Skin_id");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerTable_Table_joinCode",
                table: "PlayerTable",
                column: "Table_joinCode");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_Player_id",
                table: "RefreshToken",
                column: "Player_id");

            migrationBuilder.Sql("INSERT INTO CardReverseSkin (name, filename) VALUES ('default', 'default.png');");

            migrationBuilder.Sql(@"
    INSERT INTO Card (card)
    WITH RECURSIVE
      Ranks(Rank) AS (SELECT 2 UNION ALL SELECT Rank+1 FROM Ranks WHERE Rank < 14),
      Suits(Suit) AS (SELECT 's' UNION ALL SELECT 'c' UNION ALL SELECT 'd' UNION ALL SELECT 'h')
    SELECT CAST(Rank AS TEXT) || Suit FROM Ranks CROSS JOIN Suits;
");

            migrationBuilder.Sql(@"
    INSERT INTO CardFrontSkin (name, filename, card)
    WITH RECURSIVE
      Ranks(Rank) AS (SELECT 2 UNION ALL SELECT Rank+1 FROM Ranks WHERE Rank < 14),
      Suits(Suit) AS (SELECT 's' UNION ALL SELECT 'c' UNION ALL SELECT 'd' UNION ALL SELECT 'h')
    SELECT 
        'default_' || CAST(Rank AS TEXT) || Suit,
        'default/' || CAST(Rank AS TEXT) || Suit || '.png',
        CAST(Rank AS TEXT) || Suit
    FROM Ranks CROSS JOIN Suits;
");

            migrationBuilder.Sql(@"
    CREATE TRIGGER tr_Player_Insert_SetupAccount
    AFTER INSERT ON Player
    BEGIN
        -- 1. Domyślne skiny (już to masz)
        INSERT INTO PlayerOwnedReverseSkin (Player_id, Skin_id)
        SELECT NEW.id, id FROM CardReverseSkin WHERE name LIKE 'default%';

        INSERT INTO PlayerEquippedReverseSkin (Player_id, Skin_id)
        SELECT NEW.id, id FROM CardReverseSkin WHERE name LIKE 'default%' LIMIT 1;

        INSERT INTO PlayerOwnedFaceSkin (Player_id, Skin_id)
        SELECT NEW.id, id FROM CardFrontSkin WHERE name LIKE 'default%';

        INSERT INTO PlayerEquippedFaceSkin (Player_id, card, Skin_id)
        SELECT NEW.id, card, id FROM CardFrontSkin WHERE name LIKE 'default%';

        INSERT INTO PlayerCases (Player_id, number)
        VALUES (NEW.id, 3);
    END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerCases");

            migrationBuilder.DropTable(
                name: "PlayerEquippedFaceSkin");

            migrationBuilder.DropTable(
                name: "PlayerEquippedReverseSkin");

            migrationBuilder.DropTable(
                name: "PlayerTable");

            migrationBuilder.DropTable(
                name: "RefreshToken");

            migrationBuilder.DropTable(
                name: "PlayerOwnedFaceSkin");

            migrationBuilder.DropTable(
                name: "PlayerOwnedReverseSkin");

            migrationBuilder.DropTable(
                name: "Table");

            migrationBuilder.DropTable(
                name: "CardFrontSkin");

            migrationBuilder.DropTable(
                name: "CardReverseSkin");

            migrationBuilder.DropTable(
                name: "Player");

            migrationBuilder.DropTable(
                name: "Card");
        }
    }
}
