using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiImageGeneratorApi.Migrations
{
    /// <inheritdoc />
    public partial class update_190625_2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessageKeys_ChatMessages_TinNhanId",
                table: "ChatMessageKeys");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessageKeys_Users_ThanhVienId",
                table: "ChatMessageKeys");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatMessageKeys",
                table: "ChatMessageKeys");

            migrationBuilder.RenameTable(
                name: "ChatMessageKeys",
                newName: "ChatMessageKey");

            migrationBuilder.RenameIndex(
                name: "IX_ChatMessageKeys_TinNhanId",
                table: "ChatMessageKey",
                newName: "IX_ChatMessageKey_TinNhanId");

            migrationBuilder.RenameIndex(
                name: "IX_ChatMessageKeys_ThanhVienId",
                table: "ChatMessageKey",
                newName: "IX_ChatMessageKey_ThanhVienId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatMessageKey",
                table: "ChatMessageKey",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessageKey_ChatMessages_TinNhanId",
                table: "ChatMessageKey",
                column: "TinNhanId",
                principalTable: "ChatMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessageKey_Users_ThanhVienId",
                table: "ChatMessageKey",
                column: "ThanhVienId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessageKey_ChatMessages_TinNhanId",
                table: "ChatMessageKey");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessageKey_Users_ThanhVienId",
                table: "ChatMessageKey");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChatMessageKey",
                table: "ChatMessageKey");

            migrationBuilder.RenameTable(
                name: "ChatMessageKey",
                newName: "ChatMessageKeys");

            migrationBuilder.RenameIndex(
                name: "IX_ChatMessageKey_TinNhanId",
                table: "ChatMessageKeys",
                newName: "IX_ChatMessageKeys_TinNhanId");

            migrationBuilder.RenameIndex(
                name: "IX_ChatMessageKey_ThanhVienId",
                table: "ChatMessageKeys",
                newName: "IX_ChatMessageKeys_ThanhVienId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChatMessageKeys",
                table: "ChatMessageKeys",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessageKeys_ChatMessages_TinNhanId",
                table: "ChatMessageKeys",
                column: "TinNhanId",
                principalTable: "ChatMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessageKeys_Users_ThanhVienId",
                table: "ChatMessageKeys",
                column: "ThanhVienId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
