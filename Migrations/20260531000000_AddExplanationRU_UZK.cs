using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace propro.Migrations
{
    /// <inheritdoc />
    public partial class AddExplanationRU_UZK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExplanationUZK",
                table: "Questions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ExplanationRU",
                table: "Questions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExplanationUZK",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ExplanationRU",
                table: "Questions");
        }
    }
}
