using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWD1813.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    message_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: false),
                    project_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: true),
                    user_id = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: true),
                    content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    sent_at = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__chat_messages", x => x.message_id);
                    table.ForeignKey(
                        name: "FK_chat_messages_projects",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "project_id");
                    table.ForeignKey(
                        name: "FK_chat_messages_users",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_project_id",
                table: "chat_messages",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_sent_at",
                table: "chat_messages",
                column: "sent_at");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_user_id",
                table: "chat_messages",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chat_messages");
        }
    }
}
