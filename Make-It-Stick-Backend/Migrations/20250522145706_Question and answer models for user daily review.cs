using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Make_It_Stick_Backend.Migrations
{
    public partial class Questionandanswermodelsforuserdailyreview : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuizQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NoteId = table.Column<int>(type: "int", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Stability = table.Column<double>(type: "float", nullable: false),
                    Difficulty = table.Column<double>(type: "float", nullable: false),
                    Interval = table.Column<int>(type: "int", nullable: false),
                    LastReviewDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextReviewDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastRating = table.Column<int>(type: "int", nullable: true),
                    IsNew = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizQuestions_Notes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Notes",
                        principalColumn: "NoteId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuizReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuizQandAId = table.Column<int>(type: "int", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    StabilityAfter = table.Column<double>(type: "float", nullable: false),
                    DifficultyAfter = table.Column<double>(type: "float", nullable: false),
                    IntervalAfter = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizReviews_QuizQuestions_QuizQandAId",
                        column: x => x.QuizQandAId,
                        principalTable: "QuizQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestions_NextReviewDate",
                table: "QuizQuestions",
                column: "NextReviewDate");

            migrationBuilder.CreateIndex(
                name: "IX_QuizQuestions_NoteId",
                table: "QuizQuestions",
                column: "NoteId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizReviews_QuizQandAId",
                table: "QuizReviews",
                column: "QuizQandAId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuizReviews");

            migrationBuilder.DropTable(
                name: "QuizQuestions");
        }
    }
}
