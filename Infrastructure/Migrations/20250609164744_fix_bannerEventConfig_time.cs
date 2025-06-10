using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fix_bannerEventConfig_time : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BannerEvents_ActivePeriod",
                table: "BannerEventSpecials");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "BannerEventSpecials",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() AT TIME ZONE 'UTC'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<int>(
                name: "MaxUsagePerUser",
                table: "BannerEventSpecials",
                type: "integer",
                nullable: false,
                defaultValue: 10,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 10000);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "BannerEventSpecials",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW() AT TIME ZONE 'UTC'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.CreateIndex(
                name: "IX_BannerEvents_ActiveTimeRange",
                table: "BannerEventSpecials",
                columns: new[] { "Status", "IsActive", "StartDate", "EndDate" },
                filter: "\"Status\" = 'Active' AND \"IsActive\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BannerEvents_ActiveTimeRange",
                table: "BannerEventSpecials");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "BannerEventSpecials",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW() AT TIME ZONE 'UTC'");

            migrationBuilder.AlterColumn<int>(
                name: "MaxUsagePerUser",
                table: "BannerEventSpecials",
                type: "integer",
                nullable: false,
                defaultValue: 10000,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 10);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "BannerEventSpecials",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW() AT TIME ZONE 'UTC'");

            migrationBuilder.CreateIndex(
                name: "IX_BannerEvents_ActivePeriod",
                table: "BannerEventSpecials",
                columns: new[] { "StartDate", "EndDate", "IsActive", "Status" });
        }
    }
}
