using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiImageGeneratorApi.Migrations
{
    /// <inheritdoc />
    public partial class update_190625_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EncryptedKey",
                table: "ChatMessages",
                newName: "EncryptedKeyForSender");

            migrationBuilder.RenameColumn(
                name: "EncryptedKey",
                table: "ChatMessageDto",
                newName: "EncryptedKeyForSender");

            migrationBuilder.AddColumn<string>(
                name: "EncryptedKeyForReceiver",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedKeyForReceiver",
                table: "ChatMessageDto",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedKeyForReceiver",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "EncryptedKeyForReceiver",
                table: "ChatMessageDto");

            migrationBuilder.RenameColumn(
                name: "EncryptedKeyForSender",
                table: "ChatMessages",
                newName: "EncryptedKey");

            migrationBuilder.RenameColumn(
                name: "EncryptedKeyForSender",
                table: "ChatMessageDto",
                newName: "EncryptedKey");
        }
    }
}
