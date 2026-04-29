using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineGame.InventoryService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPowerToPlayerInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Power",
                table: "PlayerInventories",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Power",
                table: "PlayerInventories");
        }
    }
}
