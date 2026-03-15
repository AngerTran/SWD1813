using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWD1813.Migrations
{
    /// <inheritdoc />
    public partial class AddLecturerToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: false),
                    email = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    full_name = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__users__B9BE370F99F0A1FB", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "lecturers",
                columns: table => new
                {
                    lecturer_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: false),
                    user_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: true),
                    department = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__lecturer__D4D1DAB15638DEF7", x => x.lecturer_id);
                    table.ForeignKey(
                        name: "FK__lecturers__user___59FA5E80",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "groups",
                columns: table => new
                {
                    group_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: false),
                    group_name = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    lecturer_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__groups__D57795A0A10388A7", x => x.group_id);
                    table.ForeignKey(
                        name: "FK__groups__lecturer__5DCAEF64",
                        column: x => x.lecturer_id,
                        principalTable: "lecturers",
                        principalColumn: "lecturer_id");
                });

            migrationBuilder.CreateTable(
                name: "group_members",
                columns: table => new
                {
                    id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: false),
                    group_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: true),
                    user_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: true),
                    role = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    joined_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__group_me__3213E83FE9BCBDC5", x => x.id);
                    table.ForeignKey(
                        name: "FK__group_mem__group__619B8048",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "group_id");
                    table.ForeignKey(
                        name: "FK__group_mem__user___628FA481",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    project_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: false),
                    project_name = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    group_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: true),
                    jira_project_key = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__projects__BC799E1FC8A80DF7", x => x.project_id);
                    table.ForeignKey(
                        name: "FK__projects__group___66603565",
                        column: x => x.group_id,
                        principalTable: "groups",
                        principalColumn: "group_id");
                });

            migrationBuilder.CreateTable(
                name: "api_integrations",
                columns: table => new
                {
                    integration_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: false),
                    project_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: true),
                    jira_token = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    github_token = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__api_inte__B403D887F56342C5", x => x.integration_id);
                    table.ForeignKey(
                        name: "FK__api_integ__proje__06CD04F7",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "project_id");
                });

            migrationBuilder.CreateTable(
                name: "jira_issues",
                columns: table => new
                {
                    issue_id = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    project_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: true),
                    issue_key = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    issue_type = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    priority = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    assignee = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__jira_iss__D6185C3980FAD455", x => x.issue_id);
                    table.ForeignKey(
                        name: "FK__jira_issu__proje__6D0D32F4",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "project_id");
                });

            migrationBuilder.CreateTable(
                name: "reports",
                columns: table => new
                {
                    report_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: false),
                    project_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: true),
                    report_type = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    generated_by = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: true),
                    file_url = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    generated_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__reports__779B7C589DA9364A", x => x.report_id);
                    table.ForeignKey(
                        name: "FK__reports__generat__02FC7413",
                        column: x => x.generated_by,
                        principalTable: "users",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK__reports__project__02084FDA",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "project_id");
                });

            migrationBuilder.CreateTable(
                name: "repositories",
                columns: table => new
                {
                    repo_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: false),
                    project_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: true),
                    repo_name = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    repo_url = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    github_owner = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__reposito__E2D3BC802CCE93E8", x => x.repo_id);
                    table.ForeignKey(
                        name: "FK__repositor__proje__6A30C649",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "project_id");
                });

            migrationBuilder.CreateTable(
                name: "sprints",
                columns: table => new
                {
                    sprint_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: false),
                    project_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: true),
                    sprint_name = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__sprints__396C18028DF547C0", x => x.sprint_id);
                    table.ForeignKey(
                        name: "FK__sprints__project__74AE54BC",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "project_id");
                });

            migrationBuilder.CreateTable(
                name: "tasks",
                columns: table => new
                {
                    task_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: false),
                    issue_id = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    assigned_to = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: true),
                    status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    deadline = table.Column<DateOnly>(type: "date", nullable: true),
                    progress = table.Column<int>(type: "int", nullable: true, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__tasks__0492148D0EA24106", x => x.task_id);
                    table.ForeignKey(
                        name: "FK__tasks__assigned___71D1E811",
                        column: x => x.assigned_to,
                        principalTable: "users",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK__tasks__issue_id__70DDC3D8",
                        column: x => x.issue_id,
                        principalTable: "jira_issues",
                        principalColumn: "issue_id");
                });

            migrationBuilder.CreateTable(
                name: "commits",
                columns: table => new
                {
                    commit_id = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    repo_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: true),
                    author_name = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    author_email = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    commit_date = table.Column<DateTime>(type: "datetime", nullable: true),
                    files_changed = table.Column<int>(type: "int", nullable: true),
                    additions = table.Column<int>(type: "int", nullable: true),
                    deletions = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__commits__1C807873FB86E5C7", x => x.commit_id);
                    table.ForeignKey(
                        name: "FK__commits__repo_id__778AC167",
                        column: x => x.repo_id,
                        principalTable: "repositories",
                        principalColumn: "repo_id");
                });

            migrationBuilder.CreateTable(
                name: "contributor_stats",
                columns: table => new
                {
                    stat_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: false),
                    user_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: true),
                    repo_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: true),
                    total_commits = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    total_additions = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    total_deletions = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    last_commit = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__contribu__B8A52560876E2F63", x => x.stat_id);
                    table.ForeignKey(
                        name: "FK__contribut__repo___7E37BEF6",
                        column: x => x.repo_id,
                        principalTable: "repositories",
                        principalColumn: "repo_id");
                    table.ForeignKey(
                        name: "FK__contribut__user___7D439ABD",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_api_integrations_project_id",
                table: "api_integrations",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_commits_repo_id",
                table: "commits",
                column: "repo_id");

            migrationBuilder.CreateIndex(
                name: "IX_contributor_stats_repo_id",
                table: "contributor_stats",
                column: "repo_id");

            migrationBuilder.CreateIndex(
                name: "IX_contributor_stats_user_id",
                table: "contributor_stats",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_members_group_id",
                table: "group_members",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_group_members_user_id",
                table: "group_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_groups_lecturer_id",
                table: "groups",
                column: "lecturer_id");

            migrationBuilder.CreateIndex(
                name: "IX_jira_issues_project_id",
                table: "jira_issues",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "UQ__lecturer__B9BE370EB3C91E72",
                table: "lecturers",
                column: "user_id",
                unique: true,
                filter: "[user_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_projects_group_id",
                table: "projects",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_reports_generated_by",
                table: "reports",
                column: "generated_by");

            migrationBuilder.CreateIndex(
                name: "IX_reports_project_id",
                table: "reports",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_repositories_project_id",
                table: "repositories",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_sprints_project_id",
                table: "sprints",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_assigned_to",
                table: "tasks",
                column: "assigned_to");

            migrationBuilder.CreateIndex(
                name: "IX_tasks_issue_id",
                table: "tasks",
                column: "issue_id");

            migrationBuilder.CreateIndex(
                name: "UQ__users__AB6E616418C614EB",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_integrations");

            migrationBuilder.DropTable(
                name: "commits");

            migrationBuilder.DropTable(
                name: "contributor_stats");

            migrationBuilder.DropTable(
                name: "group_members");

            migrationBuilder.DropTable(
                name: "reports");

            migrationBuilder.DropTable(
                name: "sprints");

            migrationBuilder.DropTable(
                name: "tasks");

            migrationBuilder.DropTable(
                name: "repositories");

            migrationBuilder.DropTable(
                name: "jira_issues");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "groups");

            migrationBuilder.DropTable(
                name: "lecturers");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
