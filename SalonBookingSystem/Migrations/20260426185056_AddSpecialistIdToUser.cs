using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalonBookingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecialistIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SpecialistId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_SpecialistId",
                table: "Users",
                column: "SpecialistId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Specialists_SpecialistId",
                table: "Users",
                column: "SpecialistId",
                principalTable: "Specialists",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Specialists_SpecialistId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_SpecialistId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SpecialistId",
                table: "Users");
        }
    }
}
