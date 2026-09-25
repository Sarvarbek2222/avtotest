using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace propro.Migrations
{
    /// <inheritdoc />
    public partial class AddExplanationUZ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExplanationUZ",
                table: "Questions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExplanationUZ",
                table: "Questions");
        }
    }
}
