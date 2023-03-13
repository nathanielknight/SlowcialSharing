using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SlowcialSharing.Data.Migrations
{
    /// <inheritdoc />
    public partial class DedicatedRssUrlForSites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Url",
                table: "Sites",
                newName: "HomePageUrl");

            migrationBuilder.AddColumn<string>(
                name: "RssUrl",
                table: "Sites",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RssUrl",
                table: "Sites");

            migrationBuilder.RenameColumn(
                name: "HomePageUrl",
                table: "Sites",
                newName: "Url");
        }
    }
}
