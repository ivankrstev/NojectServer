using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NojectServer.Migrations
{
    /// <inheritdoc />
    public partial class ChangeUserPrimaryKeyToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_collaborators_users_user_id",
                table: "collaborators");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "collaborators",
                newName: "collaborator_id");

            migrationBuilder.RenameIndex(
                name: "IX_collaborators_user_id",
                table: "collaborators",
                newName: "IX_collaborators_collaborator_id");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "users",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(62)",
                oldMaxLength: 62);

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "users",
                type: "character varying(62)",
                maxLength: 62,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                table: "tasks",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(62)");

            migrationBuilder.AlterColumn<Guid>(
                name: "completed_by",
                table: "tasks",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(62)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "refresh_tokens",
                type: "uuid",
                maxLength: 62,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(62)",
                oldMaxLength: 62);

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                table: "projects",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(62)");

            migrationBuilder.AlterColumn<Guid>(
                name: "collaborator_id",
                table: "collaborators",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(62)");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_collaborators_users_collaborator_id",
                table: "collaborators",
                column: "collaborator_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_collaborators_users_collaborator_id",
                table: "collaborators");

            migrationBuilder.DropIndex(
                name: "IX_users_email",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email",
                table: "users");

            migrationBuilder.RenameColumn(
                name: "collaborator_id",
                table: "collaborators",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "IX_collaborators_collaborator_id",
                table: "collaborators",
                newName: "IX_collaborators_user_id");

            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                table: "users",
                type: "character varying(62)",
                maxLength: 62,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "tasks",
                type: "character varying(62)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "completed_by",
                table: "tasks",
                type: "character varying(62)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                table: "refresh_tokens",
                type: "character varying(62)",
                maxLength: 62,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldMaxLength: 62);

            migrationBuilder.AlterColumn<string>(
                name: "created_by",
                table: "projects",
                type: "character varying(62)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                table: "collaborators",
                type: "character varying(62)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_collaborators_users_user_id",
                table: "collaborators",
                column: "user_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
