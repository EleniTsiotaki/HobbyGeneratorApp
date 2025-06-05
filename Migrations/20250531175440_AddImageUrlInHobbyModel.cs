using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HobbyGeneratorAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrlInHobbyModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Hobbies",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Hobbies");
        }
    }
}
