using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rise.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PriceId",
                table: "Booking",
                type: "int",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.CreateTable(
                name: "Price",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Amount = table.Column<decimal>(
                        type: "decimal(18,2)",
                        precision: 18,
                        scale: 2,
                        nullable: false
                    ),
                    CreatedAt = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false,
                        defaultValueSql: "GETUTCDATE()"
                    ),
                    UpdatedAt = table.Column<DateTime>(
                        type: "datetime2",
                        nullable: false,
                        defaultValueSql: "GETUTCDATE()"
                    ),
                    IsDeleted = table.Column<bool>(
                        type: "bit",
                        nullable: false,
                        defaultValue: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Price", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Booking_PriceId",
                table: "Booking",
                column: "PriceId"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Booking_Price_PriceId",
                table: "Booking",
                column: "PriceId",
                principalTable: "Price",
                principalColumn: "Id"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Booking_Price_PriceId", table: "Booking");

            migrationBuilder.DropTable(name: "Price");

            migrationBuilder.DropIndex(name: "IX_Booking_PriceId", table: "Booking");

            migrationBuilder.DropColumn(name: "PriceId", table: "Booking");
        }
    }
}
