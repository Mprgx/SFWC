using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MobyPark.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Invoices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
