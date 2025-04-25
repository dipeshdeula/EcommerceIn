using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class storCh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductStores_Products_ProductId1",
                table: "ProductStores");

            migrationBuilder.DropIndex(
                name: "IX_ProductStores_ProductId1",
                table: "ProductStores");

            migrationBuilder.DropColumn(
                name: "ProductId1",
                table: "ProductStores");

            migrationBuilder.DropColumn(
                name: "StockQuantity",
                table: "ProductStores");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 4, 25, 1, 53, 16, 287, DateTimeKind.Utc).AddTicks(7675),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 4, 23, 16, 35, 3, 641, DateTimeKind.Utc).AddTicks(2881));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDateTimeUtc",
                table: "RefreshTokens",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 4, 25, 1, 53, 16, 290, DateTimeKind.Utc).AddTicks(4362),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 4, 23, 16, 35, 3, 644, DateTimeKind.Utc).AddTicks(2696));

            migrationBuilder.CreateIndex(
                name: "IX_ProductStores_StoreId_IsDeleted",
                table: "ProductStores",
                columns: new[] { "StoreId", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductStores_StoreId_IsDeleted",
                table: "ProductStores");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 4, 23, 16, 35, 3, 641, DateTimeKind.Utc).AddTicks(2881),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 4, 25, 1, 53, 16, 287, DateTimeKind.Utc).AddTicks(7675));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDateTimeUtc",
                table: "RefreshTokens",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 4, 23, 16, 35, 3, 644, DateTimeKind.Utc).AddTicks(2696),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 4, 25, 1, 53, 16, 290, DateTimeKind.Utc).AddTicks(4362));

            migrationBuilder.AddColumn<int>(
                name: "ProductId1",
                table: "ProductStores",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockQuantity",
                table: "ProductStores",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ProductStores_ProductId1",
                table: "ProductStores",
                column: "ProductId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductStores_Products_ProductId1",
                table: "ProductStores",
                column: "ProductId1",
                principalTable: "Products",
                principalColumn: "Id");
        }
    }
}
