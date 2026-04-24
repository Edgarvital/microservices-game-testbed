using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineGame.InventoryService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BaseDamage = table.Column<int>(type: "integer", nullable: false),
                    Rarity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerInventories",
                columns: table => new
                {
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseLevel = table.Column<int>(type: "integer", nullable: false),
                    BaseHp = table.Column<int>(type: "integer", nullable: false),
                    BaseAttack = table.Column<int>(type: "integer", nullable: false),
                    LastUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerInventories", x => x.PlayerId);
                });

            migrationBuilder.CreateTable(
                name: "PlayerCards",
                columns: table => new
                {
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CardId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentLevel = table.Column<int>(type: "integer", nullable: false),
                    CountDuplicates = table.Column<int>(type: "integer", nullable: false),
                    DateObtained = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUpgrade = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerCards", x => new { x.PlayerId, x.CardId });
                    table.ForeignKey(
                        name: "FK_PlayerCards_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerCards_PlayerInventories_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "PlayerInventories",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerDecks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerDecks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerDecks_PlayerInventories_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "PlayerInventories",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerDeckSlots",
                columns: table => new
                {
                    DeckId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotIndex = table.Column<int>(type: "integer", nullable: false),
                    CardId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerDeckSlots", x => new { x.DeckId, x.SlotIndex });
                    table.ForeignKey(
                        name: "FK_PlayerDeckSlots_Cards_CardId",
                        column: x => x.CardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerDeckSlots_PlayerDecks_DeckId",
                        column: x => x.DeckId,
                        principalTable: "PlayerDecks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerCards_CardId",
                table: "PlayerCards",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerDeckSlots_CardId",
                table: "PlayerDeckSlots",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerDecks_PlayerId",
                table: "PlayerDecks",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerCards");

            migrationBuilder.DropTable(
                name: "PlayerDeckSlots");

            migrationBuilder.DropTable(
                name: "Cards");

            migrationBuilder.DropTable(
                name: "PlayerDecks");

            migrationBuilder.DropTable(
                name: "PlayerInventories");
        }
    }
}
