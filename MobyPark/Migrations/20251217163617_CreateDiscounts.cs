using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MobyPark.Migrations
{
    /// <inheritdoc />
    public partial class CreateDiscounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Discounts",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValidFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ValidUntil = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    TimeWindowStart = table.Column<TimeSpan>(type: "time", nullable: true),
                    TimeWindowEnd = table.Column<TimeSpan>(type: "time", nullable: true),
                    MaxUsage = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discounts", x => x.Code);
                    table.ForeignKey(
                        name: "FK_Discounts_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DiscountCompany",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountCompany", x => new { x.Code, x.CompanyId });
                    table.ForeignKey(
                        name: "FK_DiscountCompany_Company_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Company",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiscountCompany_Discounts_Code",
                        column: x => x.Code,
                        principalTable: "Discounts",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DiscountLocation",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ParkingLotId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountLocation", x => new { x.Code, x.ParkingLotId });
                    table.ForeignKey(
                        name: "FK_DiscountLocation_Discounts_Code",
                        column: x => x.Code,
                        principalTable: "Discounts",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiscountLocation_ParkingLots_ParkingLotId",
                        column: x => x.ParkingLotId,
                        principalTable: "ParkingLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiscountUser",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountUser", x => new { x.Code, x.UserId });
                    table.ForeignKey(
                        name: "FK_DiscountUser_Discounts_Code",
                        column: x => x.Code,
                        principalTable: "Discounts",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DiscountUser_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscountCompany_CompanyId",
                table: "DiscountCompany",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountLocation_ParkingLotId",
                table: "DiscountLocation",
                column: "ParkingLotId");

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_CreatedBy",
                table: "Discounts",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountUser_UserId",
                table: "DiscountUser",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscountCompany");

            migrationBuilder.DropTable(
                name: "DiscountLocation");

            migrationBuilder.DropTable(
                name: "DiscountUser");

            migrationBuilder.DropTable(
                name: "Discounts");
        }
    }
}
