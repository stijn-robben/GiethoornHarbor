using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HarborManagementService.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusToShip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Ships",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Ships");
        }
    }
}
