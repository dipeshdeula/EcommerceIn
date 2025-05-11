using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class productDimension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 11, 9, 21, 2, 245, DateTimeKind.Utc).AddTicks(4000),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 11, 8, 18, 34, 530, DateTimeKind.Utc).AddTicks(9876));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDateTimeUtc",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 11, 9, 21, 2, 247, DateTimeKind.Utc).AddTicks(6939),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 11, 8, 18, 34, 533, DateTimeKind.Utc).AddTicks(3234));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 11, 8, 18, 34, 530, DateTimeKind.Utc).AddTicks(9876),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 11, 9, 21, 2, 245, DateTimeKind.Utc).AddTicks(4000));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDateTimeUtc",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 11, 8, 18, 34, 533, DateTimeKind.Utc).AddTicks(3234),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 11, 9, 21, 2, 247, DateTimeKind.Utc).AddTicks(6939));
        }
    }
}
