using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWD1813.Migrations
{
    /// <inheritdoc />
    public partial class AddLecturerIdToProjectsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "lecturer_id",
                table: "projects",
                type: "varchar(36)",
                unicode: false,
                maxLength: 36,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_projects_lecturer_id",
                table: "projects",
                column: "lecturer_id");

            migrationBuilder.AddForeignKey(
                name: "FK_projects_lecturers_lecturer_id",
                table: "projects",
                column: "lecturer_id",
                principalTable: "lecturers",
                principalColumn: "lecturer_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_projects_lecturers_lecturer_id",
                table: "projects");

            migrationBuilder.DropIndex(
                name: "IX_projects_lecturer_id",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "lecturer_id",
                table: "projects");
        }
    }
}
