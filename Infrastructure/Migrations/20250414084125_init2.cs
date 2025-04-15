using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 4, 14, 8, 41, 23, 623, DateTimeKind.Utc).AddTicks(9561),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 4, 13, 4, 58, 21, 68, DateTimeKind.Utc).AddTicks(3815));

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JwtId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDateTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValue: new DateTime(2025, 4, 14, 8, 41, 23, 625, DateTimeKind.Utc).AddTicks(3405)),
                    ExpiryDateTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Used = table.Column<bool>(type: "bit", nullable: false),
                    Invalidated = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(2025, 4, 13, 4, 58, 21, 68, DateTimeKind.Utc).AddTicks(3815),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(2025, 4, 14, 8, 41, 23, 623, DateTimeKind.Utc).AddTicks(9561));
        }
    }
}
