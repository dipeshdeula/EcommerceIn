using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fix_cartItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CartItems_UserId",
                table: "CartItems");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "CartItems",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "CartItems",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() AT TIME ZONE 'UTC' ",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<int>(
                name: "AppliedEventId",
                table: "CartItems",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EventDiscountAmount",
                table: "CartItems",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EventDiscountPercentage",
                table: "CartItems",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "CartItems",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsStockReserved",
                table: "CartItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityAt",
                table: "CartItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReservationToken",
                table: "CartItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReservedPrice",
                table: "CartItems",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_AppliedEventId",
                table: "CartItems",
                column: "AppliedEventId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ExpiresAt",
                table: "CartItems",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_UserId_IsDeleted",
                table: "CartItems",
                columns: new[] { "UserId", "IsDeleted" });

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_BannerEventSpecials_AppliedEventId",
                table: "CartItems",
                column: "AppliedEventId",
                principalTable: "BannerEventSpecials",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_BannerEventSpecials_AppliedEventId",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_AppliedEventId",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_ExpiresAt",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_UserId_IsDeleted",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "AppliedEventId",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "EventDiscountAmount",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "EventDiscountPercentage",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "IsStockReserved",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "LastActivityAt",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "ReservationToken",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "ReservedPrice",
                table: "CartItems");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "CartItems",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "CartItems",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW() AT TIME ZONE 'UTC' ");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_UserId",
                table: "CartItems",
                column: "UserId");
        }
    }
}
