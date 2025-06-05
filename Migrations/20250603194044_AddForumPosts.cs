using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HobbyGeneratorAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddForumPosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationUserHobby_AspNetUsers_UsersId",
                table: "ApplicationUserHobby");

            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationUserHobby_Hobbies_HobbiesId",
                table: "ApplicationUserHobby");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApplicationUserHobby",
                table: "ApplicationUserHobby");

            migrationBuilder.RenameTable(
                name: "ApplicationUserHobby",
                newName: "UserHobbies");

            migrationBuilder.RenameIndex(
                name: "IX_ApplicationUserHobby_UsersId",
                table: "UserHobbies",
                newName: "IX_UserHobbies_UsersId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserHobbies",
                table: "UserHobbies",
                columns: new[] { "HobbiesId", "UsersId" });

            migrationBuilder.CreateTable(
                name: "ForumPosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    HobbyId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForumPosts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ForumPosts_Hobbies_HobbyId",
                        column: x => x.HobbyId,
                        principalTable: "Hobbies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForumPosts_HobbyId",
                table: "ForumPosts",
                column: "HobbyId");

            migrationBuilder.CreateIndex(
                name: "IX_ForumPosts_UserId",
                table: "ForumPosts",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserHobbies_AspNetUsers_UsersId",
                table: "UserHobbies",
                column: "UsersId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserHobbies_Hobbies_HobbiesId",
                table: "UserHobbies",
                column: "HobbiesId",
                principalTable: "Hobbies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserHobbies_AspNetUsers_UsersId",
                table: "UserHobbies");

            migrationBuilder.DropForeignKey(
                name: "FK_UserHobbies_Hobbies_HobbiesId",
                table: "UserHobbies");

            migrationBuilder.DropTable(
                name: "ForumPosts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserHobbies",
                table: "UserHobbies");

            migrationBuilder.RenameTable(
                name: "UserHobbies",
                newName: "ApplicationUserHobby");

            migrationBuilder.RenameIndex(
                name: "IX_UserHobbies_UsersId",
                table: "ApplicationUserHobby",
                newName: "IX_ApplicationUserHobby_UsersId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApplicationUserHobby",
                table: "ApplicationUserHobby",
                columns: new[] { "HobbiesId", "UsersId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationUserHobby_AspNetUsers_UsersId",
                table: "ApplicationUserHobby",
                column: "UsersId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationUserHobby_Hobbies_HobbiesId",
                table: "ApplicationUserHobby",
                column: "HobbiesId",
                principalTable: "Hobbies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
