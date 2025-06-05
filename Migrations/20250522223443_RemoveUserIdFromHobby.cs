using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HobbyGeneratorAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserIdFromHobby : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Hobbies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Hobbies",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
