using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Billing.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArrivalTime",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "NeedsService",
                table: "Invoices");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Invoices",
                newName: "ShippingCompanyName");

            migrationBuilder.RenameColumn(
                name: "DepartureTime",
                table: "Invoices",
                newName: "RequestedAt");

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "Invoices");

            migrationBuilder.RenameColumn(
                name: "ShippingCompanyName",
                table: "Invoices",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "RequestedAt",
                table: "Invoices",
                newName: "DepartureTime");

            migrationBuilder.AddColumn<DateTime>(
                name: "ArrivalTime",
                table: "Invoices",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "NeedsService",
                table: "Invoices",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
