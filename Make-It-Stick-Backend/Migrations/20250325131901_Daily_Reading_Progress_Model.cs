using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Make_It_Stick_Backend.Migrations
{
    public partial class Daily_Reading_Progress_Model : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyReadingGoal",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "YearlyReadingGoal",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "CurrentPage",
                table: "LibraryItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalPages",
                table: "LibraryItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DailyProgresses",
                columns: table => new
                {
                    DailyProgressId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalMinutes = table.Column<int>(type: "int", nullable: false),
                    GoalReached = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyProgresses", x => x.DailyProgressId);
                    table.ForeignKey(
                        name: "FK_DailyProgresses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyReadingGoals",
                columns: table => new
                {
                    DailyReadingGoalId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MinutesPerDay = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyReadingGoals", x => x.DailyReadingGoalId);
                    table.ForeignKey(
                        name: "FK_DailyReadingGoals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReadingSessions",
                columns: table => new
                {
                    ReadingSessionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LibraryItemId = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartPage = table.Column<int>(type: "int", nullable: false),
                    EndPage = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: true),
                    PagesRead = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReadingSessions", x => x.ReadingSessionId);
                    table.ForeignKey(
                        name: "FK_ReadingSessions_LibraryItems_LibraryItemId",
                        column: x => x.LibraryItemId,
                        principalTable: "LibraryItems",
                        principalColumn: "LibraryItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyProgresses_UserId_Date",
                table: "DailyProgresses",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyReadingGoals_UserId",
                table: "DailyReadingGoals",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReadingSessions_LibraryItemId",
                table: "ReadingSessions",
                column: "LibraryItemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyProgresses");

            migrationBuilder.DropTable(
                name: "DailyReadingGoals");

            migrationBuilder.DropTable(
                name: "ReadingSessions");

            migrationBuilder.DropColumn(
                name: "CurrentPage",
                table: "LibraryItems");

            migrationBuilder.DropColumn(
                name: "TotalPages",
                table: "LibraryItems");

            migrationBuilder.AddColumn<int>(
                name: "DailyReadingGoal",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearlyReadingGoal",
                table: "Users",
                type: "int",
                nullable: true);
        }
    }
}
