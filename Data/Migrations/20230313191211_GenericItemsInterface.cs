using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlowcialSharing.Data.Migrations
{
    /// <inheritdoc />
    public partial class GenericItemsInterface : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Author",
                table: "Items",
                newName: "CommentsLink");

            migrationBuilder.RenameColumn(
                name: "guid",
                table: "Items",
                newName: "Key");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CommentsLink",
                table: "Items",
                newName: "Author");

            migrationBuilder.RenameColumn(
                name: "Key",
                table: "Items",
                newName: "guid");
        }
    }
}
