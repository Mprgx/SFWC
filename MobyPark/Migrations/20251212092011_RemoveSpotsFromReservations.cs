using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MobyPark.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSpotsFromReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpotsReserved",
                table: "Reservations");

            migrationBuilder.AddColumn<int>(
                name: "ReservationId",
                table: "Sessions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ReservationId",
                table: "Sessions",
                column: "ReservationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sessions_Reservations_ReservationId",
                table: "Sessions",
                column: "ReservationId",
                principalTable: "Reservations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sessions_Reservations_ReservationId",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_ReservationId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "ReservationId",
                table: "Sessions");

            migrationBuilder.AddColumn<int>(
                name: "SpotsReserved",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
