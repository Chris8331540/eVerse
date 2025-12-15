using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eVerse.Migrations
{
    /// <inheritdoc />
    public partial class RenamePropertyInSong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Order",
                table: "Songs",
                newName: "SongNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SongNumber",
                table: "Songs",
                newName: "Order");
        }
    }
}
