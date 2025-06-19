using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dock_ShipmentCompany.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShipmentCompanies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddressLine = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompanyPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompanyEmail = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentCompanies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Docks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsOccupied = table.Column<bool>(type: "bit", nullable: false),
                    IsRented = table.Column<bool>(type: "bit", nullable: false),
                    RentalStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RentalEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShipmentCompanyId = table.Column<int>(type: "int", nullable: false),
                    PricePerDay = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Docks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Docks_ShipmentCompanies_ShipmentCompanyId",
                        column: x => x.ShipmentCompanyId,
                        principalTable: "ShipmentCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DockId = table.Column<int>(type: "int", nullable: false),
                    ShipmentCompanyId = table.Column<int>(type: "int", nullable: false),
                    InvoiceMonth = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RentalStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RentalEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DaysRented = table.Column<int>(type: "int", nullable: false),
                    PricePerDay = table.Column<double>(type: "float", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Docks_DockId",
                        column: x => x.DockId,
                        principalTable: "Docks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_ShipmentCompanies_ShipmentCompanyId",
                        column: x => x.ShipmentCompanyId,
                        principalTable: "ShipmentCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Docks_ShipmentCompanyId",
                table: "Docks",
                column: "ShipmentCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_DockId_InvoiceMonth",
                table: "Invoices",
                columns: new[] { "DockId", "InvoiceMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_ShipmentCompanyId",
                table: "Invoices",
                column: "ShipmentCompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "Docks");

            migrationBuilder.DropTable(
                name: "ShipmentCompanies");
        }
    }
}
