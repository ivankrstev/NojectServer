using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NojectServer.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "character varying(62)", maxLength: 62, nullable: false),
                    token = table.Column<string>(type: "text", nullable: false),
                    valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => new { x.user_id, x.token });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refresh_tokens");
        }
    }
}
