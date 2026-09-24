using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NojectServer.Modules.Identity.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddUserAndRefreshTokens : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                normalized_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                full_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                password_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                password_salt = table.Column<byte[]>(type: "bytea", nullable: false),
                verification_token_hash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: true),
                verification_token_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                password_reset_token_hash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: true),
                password_reset_token_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                protected_two_factor_secret = table.Column<byte[]>(type: "bytea", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_users", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "refresh_tokens",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                token_hash = table.Column<byte[]>(type: "bytea", maxLength: 32, nullable: false),
                family_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                replaced_by_token_id = table.Column<Guid>(type: "uuid", nullable: true),
                concurrency_token = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_refresh_tokens", x => x.id);
                table.ForeignKey(
                    name: "fk_refresh_tokens_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_refresh_tokens_token_hash",
            table: "refresh_tokens",
            column: "token_hash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_refresh_tokens_user_id_family_id",
            table: "refresh_tokens",
            columns: new[] { "user_id", "family_id" });

        migrationBuilder.CreateIndex(
            name: "ix_users_normalized_email",
            table: "users",
            column: "normalized_email",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "refresh_tokens");

        migrationBuilder.DropTable(
            name: "users");
    }
}
