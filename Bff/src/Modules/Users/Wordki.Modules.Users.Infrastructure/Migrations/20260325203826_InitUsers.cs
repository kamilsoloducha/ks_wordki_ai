using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wordki.Modules.Users.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "users");

            migrationBuilder.CreateTable(
                name: "shared_event_messages",
                schema: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    publisher_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    consumer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    data_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    added_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    handled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payload = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shared_event_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    user_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EmailConfirmedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmailConfirmationTokenHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    EmailConfirmationTokenExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "integer", nullable: false),
                    LockoutEndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLoginAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SecurityStamp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_shared_event_messages_added_at_utc",
                schema: "users",
                table: "shared_event_messages",
                column: "added_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_shared_event_messages_handled_at_utc",
                schema: "users",
                table: "shared_event_messages",
                column: "handled_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_users_DeletedAtUtc",
                schema: "users",
                table: "users",
                column: "DeletedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_users_NormalizedEmail",
                schema: "users",
                table: "users",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Status",
                schema: "users",
                table: "users",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shared_event_messages",
                schema: "users");

            migrationBuilder.DropTable(
                name: "users",
                schema: "users");
        }
    }
}
