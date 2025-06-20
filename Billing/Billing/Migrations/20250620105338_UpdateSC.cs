using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArrivalTime",
                table: "shippingCompanies");

            migrationBuilder.DropColumn(
                name: "DepartureTime",
                table: "shippingCompanies");

            migrationBuilder.DropColumn(
                name: "NeedsService",
                table: "shippingCompanies");

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "shippingCompanies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShipmentCompanyCardNumber",
                table: "shippingCompanies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "shippingCompanies");

            migrationBuilder.DropColumn(
                name: "ShipmentCompanyCardNumber",
                table: "shippingCompanies");

            migrationBuilder.AddColumn<DateTime>(
                name: "ArrivalTime",
                table: "shippingCompanies",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DepartureTime",
                table: "shippingCompanies",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "NeedsService",
                table: "shippingCompanies",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
