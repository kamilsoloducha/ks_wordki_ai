using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Wordki.Bff.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cards");

            migrationBuilder.EnsureSchema(
                name: "lessons");

            migrationBuilder.EnsureSchema(
                name: "users");

            migrationBuilder.CreateTable(
                name: "card_sides",
                schema: "cards",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    example = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_card_sides", x => x.id);
                });

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
                schema: "cards",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "lessons",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    external_user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users1", x => x.id);
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
                    table.PrimaryKey("PK_users2", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "groups",
                schema: "cards",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    front_side_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    back_side_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_groups", x => x.id);
                    table.ForeignKey(
                        name: "FK_groups_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "cards",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "results",
                schema: "cards",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    group_id = table.Column<long>(type: "bigint", nullable: false),
                    card_side_id = table.Column<long>(type: "bigint", nullable: false),
                    drawer = table.Column<int>(type: "integer", nullable: false),
                    next_repeat_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    counter = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_results", x => x.id);
                    table.ForeignKey(
                        name: "FK_results_card_sides_card_side_id",
                        column: x => x.card_side_id,
                        principalSchema: "cards",
                        principalTable: "card_sides",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_results_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "cards",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lessons",
                schema: "lessons",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    lesson_kind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    word_count = table.Column<int>(type: "integer", nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lessons", x => x.id);
                    table.ForeignKey(
                        name: "FK_lessons_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "lessons",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cards",
                schema: "cards",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_id = table.Column<long>(type: "bigint", nullable: false),
                    front_side_id = table.Column<long>(type: "bigint", nullable: false),
                    back_side_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cards", x => x.id);
                    table.ForeignKey(
                        name: "FK_cards_card_sides_back_side_id",
                        column: x => x.back_side_id,
                        principalSchema: "cards",
                        principalTable: "card_sides",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cards_card_sides_front_side_id",
                        column: x => x.front_side_id,
                        principalSchema: "cards",
                        principalTable: "card_sides",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_cards_groups_group_id",
                        column: x => x.group_id,
                        principalSchema: "cards",
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lesson_repetitions",
                schema: "lessons",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    lesson_id = table.Column<long>(type: "bigint", nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    question_result_id = table.Column<long>(type: "bigint", nullable: false),
                    is_known = table.Column<bool>(type: "boolean", nullable: false),
                    answered_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lesson_repetitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_lesson_repetitions_lessons_lesson_id",
                        column: x => x.lesson_id,
                        principalSchema: "lessons",
                        principalTable: "lessons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cards_back_side_id",
                schema: "cards",
                table: "cards",
                column: "back_side_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cards_front_side_id",
                schema: "cards",
                table: "cards",
                column: "front_side_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cards_group_id",
                schema: "cards",
                table: "cards",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_groups_user_id",
                schema: "cards",
                table: "groups",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_lesson_repetitions_lesson_id",
                schema: "lessons",
                table: "lesson_repetitions",
                column: "lesson_id");

            migrationBuilder.CreateIndex(
                name: "IX_lesson_repetitions_lesson_id_sequence_number",
                schema: "lessons",
                table: "lesson_repetitions",
                columns: new[] { "lesson_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lesson_repetitions_question_result_id",
                schema: "lessons",
                table: "lesson_repetitions",
                column: "question_result_id");

            migrationBuilder.CreateIndex(
                name: "IX_lessons_user_id",
                schema: "lessons",
                table: "lessons",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_results_card_side_id",
                schema: "cards",
                table: "results",
                column: "card_side_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_results_group_id",
                schema: "cards",
                table: "results",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "IX_results_user_id",
                schema: "cards",
                table: "results",
                column: "user_id");

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
                name: "IX_users_external_user_id",
                schema: "cards",
                table: "users",
                column: "external_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_external_user_id1",
                schema: "lessons",
                table: "users",
                column: "external_user_id",
                unique: true);

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
                name: "cards",
                schema: "cards");

            migrationBuilder.DropTable(
                name: "lesson_repetitions",
                schema: "lessons");

            migrationBuilder.DropTable(
                name: "results",
                schema: "cards");

            migrationBuilder.DropTable(
                name: "shared_event_messages",
                schema: "users");

            migrationBuilder.DropTable(
                name: "users",
                schema: "users");

            migrationBuilder.DropTable(
                name: "groups",
                schema: "cards");

            migrationBuilder.DropTable(
                name: "lessons",
                schema: "lessons");

            migrationBuilder.DropTable(
                name: "card_sides",
                schema: "cards");

            migrationBuilder.DropTable(
                name: "users",
                schema: "cards");

            migrationBuilder.DropTable(
                name: "users",
                schema: "lessons");
        }
    }
}
