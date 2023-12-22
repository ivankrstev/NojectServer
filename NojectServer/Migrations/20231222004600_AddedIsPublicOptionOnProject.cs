using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NojectServer.Migrations
{
    /// <inheritdoc />
    public partial class AddedIsPublicOptionOnProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_public",
                table: "projects",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_public",
                table: "projects");
        }
    }
}
