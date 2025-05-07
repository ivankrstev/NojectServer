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
            // Drop foreign key constraints
            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_users_user_id",
                table: "refresh_tokens");

            migrationBuilder.DropForeignKey(
                name: "FK_projects_users_created_by",
                table: "projects");

            migrationBuilder.DropForeignKey(
                name: "FK_collaborators_users_user_id",
                table: "collaborators");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_users_created_by",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_users_completed_by",
                table: "tasks");

            // Since we're in development mode and don't care about data,
            // truncate all tables to avoid FK constraint issues
            migrationBuilder.Sql("TRUNCATE TABLE collaborators, tasks, refresh_tokens, projects, users CASCADE");

            // Rename column in collaborators
            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "collaborators",
                newName: "collaborator_id");

            migrationBuilder.RenameIndex(
                name: "IX_collaborators_user_id",
                table: "collaborators",
                newName: "IX_collaborators_collaborator_id");

            // Add email column to users table
            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "users",
                type: "character varying(62)",
                maxLength: 62,
                nullable: false,
                defaultValue: "");

            // Create index on email
            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            // Change all columns to exactly the same type specification
            // Using the explicit "uuid" type for all columns involved in FK relationships
            migrationBuilder.Sql("ALTER TABLE users ALTER COLUMN user_id TYPE uuid USING gen_random_uuid()");
            migrationBuilder.Sql("ALTER TABLE tasks ALTER COLUMN created_by TYPE uuid USING gen_random_uuid()");
            migrationBuilder.Sql("ALTER TABLE tasks ALTER COLUMN completed_by TYPE uuid USING null");
            migrationBuilder.Sql("ALTER TABLE refresh_tokens ALTER COLUMN user_id TYPE uuid USING gen_random_uuid()");
            migrationBuilder.Sql("ALTER TABLE projects ALTER COLUMN created_by TYPE uuid USING gen_random_uuid()");
            migrationBuilder.Sql("ALTER TABLE collaborators ALTER COLUMN collaborator_id TYPE uuid USING gen_random_uuid()");

            // Add back the foreign key constraints explicitly with matching column types
            migrationBuilder.Sql(@"
                ALTER TABLE collaborators
                ADD CONSTRAINT FK_collaborators_users_collaborator_id
                FOREIGN KEY (collaborator_id)
                REFERENCES users(user_id) ON DELETE CASCADE
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE tasks
                ADD CONSTRAINT FK_tasks_users_created_by
                FOREIGN KEY (created_by)
                REFERENCES users(user_id) ON DELETE CASCADE
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE tasks
                ADD CONSTRAINT FK_tasks_users_completed_by
                FOREIGN KEY (completed_by)
                REFERENCES users(user_id) ON DELETE NO ACTION
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE refresh_tokens
                ADD CONSTRAINT FK_refresh_tokens_users_user_id
                FOREIGN KEY (user_id)
                REFERENCES users(user_id) ON DELETE CASCADE
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE projects
                ADD CONSTRAINT FK_projects_users_created_by
                FOREIGN KEY (created_by)
                REFERENCES users(user_id) ON DELETE CASCADE
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop all foreign keys first
            migrationBuilder.Sql("ALTER TABLE collaborators DROP CONSTRAINT IF EXISTS FK_collaborators_users_collaborator_id");
            migrationBuilder.Sql("ALTER TABLE tasks DROP CONSTRAINT IF EXISTS FK_tasks_users_created_by");
            migrationBuilder.Sql("ALTER TABLE tasks DROP CONSTRAINT IF EXISTS FK_tasks_users_completed_by");
            migrationBuilder.Sql("ALTER TABLE refresh_tokens DROP CONSTRAINT IF EXISTS FK_refresh_tokens_users_user_id");
            migrationBuilder.Sql("ALTER TABLE projects DROP CONSTRAINT IF EXISTS FK_projects_users_created_by");

            // Truncate tables for clean downgrade
            migrationBuilder.Sql("TRUNCATE TABLE collaborators, tasks, refresh_tokens, projects, users CASCADE");

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

            // Convert back to string type
            migrationBuilder.Sql("ALTER TABLE users ALTER COLUMN user_id TYPE character varying(62) USING 'user-' || user_id::text");
            migrationBuilder.Sql("ALTER TABLE tasks ALTER COLUMN created_by TYPE character varying(62) USING 'user-' || created_by::text");
            migrationBuilder.Sql("ALTER TABLE tasks ALTER COLUMN completed_by TYPE character varying(62) USING NULL");
            migrationBuilder.Sql("ALTER TABLE refresh_tokens ALTER COLUMN user_id TYPE character varying(62) USING 'user-' || user_id::text");
            migrationBuilder.Sql("ALTER TABLE projects ALTER COLUMN created_by TYPE character varying(62) USING 'user-' || created_by::text");
            migrationBuilder.Sql("ALTER TABLE collaborators ALTER COLUMN user_id TYPE character varying(62) USING 'user-' || user_id::text");

            // Re-add foreign keys with explicit string types
            migrationBuilder.Sql(@"
                ALTER TABLE collaborators
                ADD CONSTRAINT FK_collaborators_users_user_id
                FOREIGN KEY (user_id)
                REFERENCES users(user_id) ON DELETE CASCADE
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE tasks
                ADD CONSTRAINT FK_tasks_users_created_by
                FOREIGN KEY (created_by)
                REFERENCES users(user_id) ON DELETE CASCADE
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE tasks
                ADD CONSTRAINT FK_tasks_users_completed_by
                FOREIGN KEY (completed_by)
                REFERENCES users(user_id) ON DELETE NO ACTION
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE refresh_tokens
                ADD CONSTRAINT FK_refresh_tokens_users_user_id
                FOREIGN KEY (user_id)
                REFERENCES users(user_id) ON DELETE CASCADE
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE projects
                ADD CONSTRAINT FK_projects_users_created_by
                FOREIGN KEY (created_by)
                REFERENCES users(user_id) ON DELETE CASCADE
            ");
        }
    }
}
