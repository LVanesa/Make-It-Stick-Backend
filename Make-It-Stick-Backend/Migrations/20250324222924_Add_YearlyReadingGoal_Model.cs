using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Make_It_Stick_Backend.Migrations
{
    public partial class Add_YearlyReadingGoal_Model : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Books_YearlyReadingGoal_YearlyReadingGoalId",
                table: "Books");

            migrationBuilder.DropForeignKey(
                name: "FK_YearlyReadingGoal_Users_UserId",
                table: "YearlyReadingGoal");

            migrationBuilder.DropPrimaryKey(
                name: "PK_YearlyReadingGoal",
                table: "YearlyReadingGoal");

            migrationBuilder.RenameTable(
                name: "YearlyReadingGoal",
                newName: "YearlyReadingGoals");

            migrationBuilder.RenameIndex(
                name: "IX_YearlyReadingGoal_UserId",
                table: "YearlyReadingGoals",
                newName: "IX_YearlyReadingGoals_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_YearlyReadingGoals",
                table: "YearlyReadingGoals",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Books_YearlyReadingGoals_YearlyReadingGoalId",
                table: "Books",
                column: "YearlyReadingGoalId",
                principalTable: "YearlyReadingGoals",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_YearlyReadingGoals_Users_UserId",
                table: "YearlyReadingGoals",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Books_YearlyReadingGoals_YearlyReadingGoalId",
                table: "Books");

            migrationBuilder.DropForeignKey(
                name: "FK_YearlyReadingGoals_Users_UserId",
                table: "YearlyReadingGoals");

            migrationBuilder.DropPrimaryKey(
                name: "PK_YearlyReadingGoals",
                table: "YearlyReadingGoals");

            migrationBuilder.RenameTable(
                name: "YearlyReadingGoals",
                newName: "YearlyReadingGoal");

            migrationBuilder.RenameIndex(
                name: "IX_YearlyReadingGoals_UserId",
                table: "YearlyReadingGoal",
                newName: "IX_YearlyReadingGoal_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_YearlyReadingGoal",
                table: "YearlyReadingGoal",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Books_YearlyReadingGoal_YearlyReadingGoalId",
                table: "Books",
                column: "YearlyReadingGoalId",
                principalTable: "YearlyReadingGoal",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_YearlyReadingGoal_Users_UserId",
                table: "YearlyReadingGoal",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
