using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Make_It_Stick_Backend.Migrations
{
    public partial class AddBaseNoteTagDbSet : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BaseNoteTag_Notes_NoteId",
                table: "BaseNoteTag");

            migrationBuilder.DropForeignKey(
                name: "FK_BaseNoteTag_Tags_TagId",
                table: "BaseNoteTag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BaseNoteTag",
                table: "BaseNoteTag");

            migrationBuilder.RenameTable(
                name: "BaseNoteTag",
                newName: "BaseNoteTags");

            migrationBuilder.RenameIndex(
                name: "IX_BaseNoteTag_TagId",
                table: "BaseNoteTags",
                newName: "IX_BaseNoteTags_TagId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BaseNoteTags",
                table: "BaseNoteTags",
                columns: new[] { "NoteId", "TagId" });

            migrationBuilder.AddForeignKey(
                name: "FK_BaseNoteTags_Notes_NoteId",
                table: "BaseNoteTags",
                column: "NoteId",
                principalTable: "Notes",
                principalColumn: "NoteId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BaseNoteTags_Tags_TagId",
                table: "BaseNoteTags",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "TagId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BaseNoteTags_Notes_NoteId",
                table: "BaseNoteTags");

            migrationBuilder.DropForeignKey(
                name: "FK_BaseNoteTags_Tags_TagId",
                table: "BaseNoteTags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BaseNoteTags",
                table: "BaseNoteTags");

            migrationBuilder.RenameTable(
                name: "BaseNoteTags",
                newName: "BaseNoteTag");

            migrationBuilder.RenameIndex(
                name: "IX_BaseNoteTags_TagId",
                table: "BaseNoteTag",
                newName: "IX_BaseNoteTag_TagId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BaseNoteTag",
                table: "BaseNoteTag",
                columns: new[] { "NoteId", "TagId" });

            migrationBuilder.AddForeignKey(
                name: "FK_BaseNoteTag_Notes_NoteId",
                table: "BaseNoteTag",
                column: "NoteId",
                principalTable: "Notes",
                principalColumn: "NoteId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_BaseNoteTag_Tags_TagId",
                table: "BaseNoteTag",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "TagId");
        }
    }
}
