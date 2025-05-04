using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class chgPrRe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 4, 16, 22, 56, 416, DateTimeKind.Utc).AddTicks(2221),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 2, 7, 47, 41, 553, DateTimeKind.Utc).AddTicks(3207));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDateTimeUtc",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 4, 16, 22, 56, 418, DateTimeKind.Utc).AddTicks(3602),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 2, 7, 47, 41, 555, DateTimeKind.Utc).AddTicks(5336));

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Products",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Products");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 2, 7, 47, 41, 553, DateTimeKind.Utc).AddTicks(3207),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 4, 16, 22, 56, 416, DateTimeKind.Utc).AddTicks(2221));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDateTimeUtc",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 2, 7, 47, 41, 555, DateTimeKind.Utc).AddTicks(5336),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 4, 16, 22, 56, 418, DateTimeKind.Utc).AddTicks(3602));
        }
    }
}
