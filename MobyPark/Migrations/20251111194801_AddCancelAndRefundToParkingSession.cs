using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MobyPark.Migrations
{
    /// <inheritdoc />
    public partial class AddCancelAndRefundToParkingSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAt",
                table: "ParkingSessions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCancelled",
                table: "ParkingSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRefunded",
                table: "ParkingSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RefundDate",
                table: "ParkingSessions",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "ParkingSessions");

            migrationBuilder.DropColumn(
                name: "IsCancelled",
                table: "ParkingSessions");

            migrationBuilder.DropColumn(
                name: "IsRefunded",
                table: "ParkingSessions");

            migrationBuilder.DropColumn(
                name: "RefundDate",
                table: "ParkingSessions");
        }
    }
}
