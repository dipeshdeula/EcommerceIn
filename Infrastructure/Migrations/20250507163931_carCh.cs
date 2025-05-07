using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class carCh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 7, 16, 39, 30, 662, DateTimeKind.Utc).AddTicks(9260),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 7, 13, 18, 20, 179, DateTimeKind.Utc).AddTicks(5601));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDateTimeUtc",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 7, 16, 39, 30, 667, DateTimeKind.Utc).AddTicks(2831),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 7, 13, 18, 20, 183, DateTimeKind.Utc).AddTicks(6971));

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "CartItems",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                table: "CartItems",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "CartItems",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 7, 13, 18, 20, 179, DateTimeKind.Utc).AddTicks(5601),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 7, 16, 39, 30, 662, DateTimeKind.Utc).AddTicks(9260));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDateTimeUtc",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 7, 13, 18, 20, 183, DateTimeKind.Utc).AddTicks(6971),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 7, 16, 39, 30, 667, DateTimeKind.Utc).AddTicks(2831));

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "CartItems",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                table: "CartItems",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "CartItems",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");
        }
    }
}
