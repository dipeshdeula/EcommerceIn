using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class initBannerImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "BannerEventSpecials");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 13, 11, 33, 45, 871, DateTimeKind.Utc).AddTicks(8250),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 12, 9, 30, 58, 71, DateTimeKind.Utc).AddTicks(2002));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDateTimeUtc",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 13, 11, 33, 45, 874, DateTimeKind.Utc).AddTicks(4439),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 12, 9, 30, 58, 73, DateTimeKind.Utc).AddTicks(5518));

            migrationBuilder.CreateTable(
                name: "BannerImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BannerId = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsMain = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BannerImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BannerImages_BannerEventSpecials_BannerId",
                        column: x => x.BannerId,
                        principalTable: "BannerEventSpecials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BannerImages_BannerId_IsMain",
                table: "BannerImages",
                columns: new[] { "BannerId", "IsMain" },
                unique: true,
                filter: "\"IsMain\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BannerImages");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 12, 9, 30, 58, 71, DateTimeKind.Utc).AddTicks(2002),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 13, 11, 33, 45, 871, DateTimeKind.Utc).AddTicks(8250));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDateTimeUtc",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2025, 5, 12, 9, 30, 58, 73, DateTimeKind.Utc).AddTicks(5518),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2025, 5, 13, 11, 33, 45, 874, DateTimeKind.Utc).AddTicks(4439));

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "BannerEventSpecials",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");
        }
    }
}
