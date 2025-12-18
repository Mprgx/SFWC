using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MobyPark.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDiscountLogic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountWithDiscount",
                table: "Payments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DiscountCode",
                table: "Payments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentUsage",
                table: "Discounts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_DiscountCode",
                table: "Payments",
                column: "DiscountCode");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Discounts_DiscountCode",
                table: "Payments",
                column: "DiscountCode",
                principalTable: "Discounts",
                principalColumn: "Code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Discounts_DiscountCode",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_DiscountCode",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AmountWithDiscount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DiscountCode",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CurrentUsage",
                table: "Discounts");
        }
    }
}
