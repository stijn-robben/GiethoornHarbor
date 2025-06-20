using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCompanyName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "shippingCompanies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "shippingCompanies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
