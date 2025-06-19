using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiImageGeneratorApi.Migrations
{
    /// <inheritdoc />
    public partial class update100625 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrivateKey",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PublicKey",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EncryptedKey",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EncryptedMessage",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IV",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EncryptedKey",
                table: "ChatMessageDto",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EncryptedMessage",
                table: "ChatMessageDto",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IV",
                table: "ChatMessageDto",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_NguoiGuiId",
                table: "ChatMessages",
                column: "NguoiGuiId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_NguoiNhanId",
                table: "ChatMessages",
                column: "NguoiNhanId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Users_NguoiGuiId",
                table: "ChatMessages",
                column: "NguoiGuiId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Users_NguoiNhanId",
                table: "ChatMessages",
                column: "NguoiNhanId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_Users_NguoiGuiId",
                table: "ChatMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_Users_NguoiNhanId",
                table: "ChatMessages");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_NguoiGuiId",
                table: "ChatMessages");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_NguoiNhanId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "PrivateKey",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PublicKey",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EncryptedKey",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "EncryptedMessage",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "IV",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "EncryptedKey",
                table: "ChatMessageDto");

            migrationBuilder.DropColumn(
                name: "EncryptedMessage",
                table: "ChatMessageDto");

            migrationBuilder.DropColumn(
                name: "IV",
                table: "ChatMessageDto");
        }
    }
}
