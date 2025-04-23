using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class storeAd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductImages_ProductId",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Stores");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 4, 23, 16, 20, 28, 523, DateTimeKind.Utc).AddTicks(518),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 4, 22, 8, 39, 13, 989, DateTimeKind.Utc).AddTicks(8159));

            migrationBuilder.AddColumn<string>(
                name: "StoreCity",
                table: "Stores",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDateTimeUtc",
                table: "RefreshTokens",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 4, 23, 16, 20, 28, 526, DateTimeKind.Utc).AddTicks(7219),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 4, 22, 8, 39, 13, 991, DateTimeKind.Utc).AddTicks(4942));

            migrationBuilder.AddColumn<int>(
                name: "ProductId1",
                table: "ProductStores",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsMain",
                table: "ProductImages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "StoreAddresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    Street = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Province = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreAddresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreAddresses_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductStores_ProductId1",
                table: "ProductStores",
                column: "ProductId1");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId_IsMain",
                table: "ProductImages",
                columns: new[] { "ProductId", "IsMain" },
                unique: true,
                filter: "[IsMain] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_StoreAddresses_StoreId",
                table: "StoreAddresses",
                column: "StoreId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductStores_Products_ProductId1",
                table: "ProductStores",
                column: "ProductId1",
                principalTable: "Products",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductStores_Products_ProductId1",
                table: "ProductStores");

            migrationBuilder.DropTable(
                name: "StoreAddresses");

            migrationBuilder.DropIndex(
                name: "IX_ProductStores_ProductId1",
                table: "ProductStores");

            migrationBuilder.DropIndex(
                name: "IX_ProductImages_ProductId_IsMain",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "StoreCity",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "ProductId1",
                table: "ProductStores");

            migrationBuilder.DropColumn(
                name: "IsMain",
                table: "ProductImages");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 4, 22, 8, 39, 13, 989, DateTimeKind.Utc).AddTicks(8159),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 4, 23, 16, 20, 28, 523, DateTimeKind.Utc).AddTicks(518));

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Stores",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Stores",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDateTimeUtc",
                table: "RefreshTokens",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 4, 22, 8, 39, 13, 991, DateTimeKind.Utc).AddTicks(4942),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 4, 23, 16, 20, 28, 526, DateTimeKind.Utc).AddTicks(7219));

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId",
                table: "ProductImages",
                column: "ProductId");
        }
    }
}
