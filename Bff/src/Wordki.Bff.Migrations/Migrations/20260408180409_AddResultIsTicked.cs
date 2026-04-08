using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wordki.Bff.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddResultIsTicked : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_ticked",
                schema: "cards",
                table: "results",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_ticked",
                schema: "cards",
                table: "results");
        }
    }
}
