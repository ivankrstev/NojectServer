using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NojectServer.Migrations
{
    /// <inheritdoc />
    public partial class AddedTwoFactorAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "tfa_enabled",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "tfa_secret_key",
                table: "users",
                type: "char(32)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tfa_enabled",
                table: "users");

            migrationBuilder.DropColumn(
                name: "tfa_secret_key",
                table: "users");
        }
    }
}
