using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeachSpace.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGradeFromTrainee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Grade",
                table: "Trainees");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Grade",
                table: "Trainees",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
