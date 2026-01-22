using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MobyPark.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthYearToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Date",
                table: "Invoices",
                newName: "DateRequested");

            migrationBuilder.AddColumn<int>(
                name: "Month",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Month",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "Invoices");

            migrationBuilder.RenameColumn(
                name: "DateRequested",
                table: "Invoices",
                newName: "Date");
        }
    }
}
