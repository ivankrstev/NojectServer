using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NojectServer.Migrations
{
    /// <inheritdoc />
    public partial class FixedProjectTaskForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_projects_tasks_first_task",
                table: "projects");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_tasks_next",
                table: "tasks");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_tasks_task_id",
                table: "tasks");

            migrationBuilder.DropIndex(
                name: "IX_tasks_next",
                table: "tasks");

            migrationBuilder.DropIndex(
                name: "IX_projects_first_task",
                table: "projects");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_next_project_id",
                table: "tasks",
                columns: new[] { "next", "project_id" });

            migrationBuilder.CreateIndex(
                name: "IX_projects_first_task_project_id",
                table: "projects",
                columns: new[] { "first_task", "project_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_projects_tasks_first_task_project_id",
                table: "projects",
                columns: new[] { "first_task", "project_id" },
                principalTable: "tasks",
                principalColumns: new[] { "task_id", "project_id" });

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_tasks_next_project_id",
                table: "tasks",
                columns: new[] { "next", "project_id" },
                principalTable: "tasks",
                principalColumns: new[] { "task_id", "project_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_projects_tasks_first_task_project_id",
                table: "projects");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_tasks_next_project_id",
                table: "tasks");

            migrationBuilder.DropIndex(
                name: "IX_tasks_next_project_id",
                table: "tasks");

            migrationBuilder.DropIndex(
                name: "IX_projects_first_task_project_id",
                table: "projects");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_tasks_task_id",
                table: "tasks",
                column: "task_id");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_next",
                table: "tasks",
                column: "next");

            migrationBuilder.CreateIndex(
                name: "IX_projects_first_task",
                table: "projects",
                column: "first_task");

            migrationBuilder.AddForeignKey(
                name: "FK_projects_tasks_first_task",
                table: "projects",
                column: "first_task",
                principalTable: "tasks",
                principalColumn: "task_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_tasks_next",
                table: "tasks",
                column: "next",
                principalTable: "tasks",
                principalColumn: "task_id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
