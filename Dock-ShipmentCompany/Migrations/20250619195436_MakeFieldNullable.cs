using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dock_ShipmentCompany.Migrations
{
    /// <inheritdoc />
    public partial class MakeFieldNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Docks_ShipmentCompanies_ShipmentCompanyId",
                table: "Docks");

            migrationBuilder.AlterColumn<int>(
                name: "ShipmentCompanyId",
                table: "Docks",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Docks_ShipmentCompanies_ShipmentCompanyId",
                table: "Docks",
                column: "ShipmentCompanyId",
                principalTable: "ShipmentCompanies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Docks_ShipmentCompanies_ShipmentCompanyId",
                table: "Docks");

            migrationBuilder.AlterColumn<int>(
                name: "ShipmentCompanyId",
                table: "Docks",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Docks_ShipmentCompanies_ShipmentCompanyId",
                table: "Docks",
                column: "ShipmentCompanyId",
                principalTable: "ShipmentCompanies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
