using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MRPCP_Product : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 17, 4, 21, 44, 91, DateTimeKind.Utc).AddTicks(4194),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 16, 16, 20, 58, 206, DateTimeKind.Utc).AddTicks(5940));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDateTimeUtc",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 17, 4, 21, 44, 104, DateTimeKind.Utc).AddTicks(2439),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 16, 16, 20, 58, 208, DateTimeKind.Utc).AddTicks(9509));

            migrationBuilder.AlterColumn<double>(
                name: "MarketPrice",
                table: "Products",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "decimal(18,2");

            migrationBuilder.AlterColumn<double>(
                name: "DiscountPrice",
                table: "Products",
                type: "numeric(18,2)",
                nullable: true,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "numeric(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "CostPrice",
                table: "Products",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "numeric(18,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 16, 16, 20, 58, 206, DateTimeKind.Utc).AddTicks(5940),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 17, 4, 21, 44, 91, DateTimeKind.Utc).AddTicks(4194));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDateTimeUtc",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 16, 16, 20, 58, 208, DateTimeKind.Utc).AddTicks(9509),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 17, 4, 21, 44, 104, DateTimeKind.Utc).AddTicks(2439));

            migrationBuilder.AlterColumn<double>(
                name: "MarketPrice",
                table: "Products",
                type: "decimal(18,2",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "numeric(18,2)",
                oldDefaultValue: 0.0);

            migrationBuilder.AlterColumn<double>(
                name: "DiscountPrice",
                table: "Products",
                type: "numeric(18,2)",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "numeric(18,2)",
                oldNullable: true,
                oldDefaultValue: 0.0);

            migrationBuilder.AlterColumn<double>(
                name: "CostPrice",
                table: "Products",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "numeric(18,2)",
                oldDefaultValue: 0.0);
        }
    }
}
