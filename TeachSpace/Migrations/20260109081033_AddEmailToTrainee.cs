using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeachSpace.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailToTrainee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Trainees",
                type: "nvarchar(450)",
                nullable: true);

            //  Unique Index with Filter (ignore NULL)
            migrationBuilder.CreateIndex(
                name: "IX_Trainees_Email",
                table: "Trainees",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trainees_Email",
                table: "Trainees");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Trainees");
        }
    }
}
