using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Product_DiscountPercentage_fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.RenameColumn(
            //    name: "RowVersion",
            //    table: "Notifications",
            //    newName: "xmin");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercentage",
                table: "Products",
                type: "numeric",
                nullable: true);

            migrationBuilder.AlterColumn<uint>(
                name: "xmin",
                table: "Notifications",
                type: "xid",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldRowVersion: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountPercentage",
                table: "Products");

            //migrationBuilder.RenameColumn(
            //    name: "xmin",
            //    table: "Notifications",
            //    newName: "RowVersion");

            //migrationBuilder.AlterColumn<byte[]>(
            //    name: "RowVersion",
            //    table: "Notifications",
            //    type: "bytea",
            //    rowVersion: true,
            //    nullable: false,
            //    oldClrType: typeof(uint),
            //    oldType: "xid",
            //    oldRowVersion: true);
        }
    }
}
