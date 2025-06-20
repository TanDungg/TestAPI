using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiImageGeneratorApi.Migrations
{
    /// <inheritdoc />
    public partial class update_200625 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedKeyForReceiver",
                table: "ChatMessageDto");

            migrationBuilder.RenameColumn(
                name: "EncryptedKeyForSender",
                table: "ChatMessageDto",
                newName: "EncryptedKey");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ChatMessageKey",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "ChatMessageKey",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ChatMessageKey",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "ChatMessageKey",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ChatMessageKey",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ChatMessageKey",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "ChatMessageKey",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ChatMessageKey");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ChatMessageKey");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ChatMessageKey");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ChatMessageKey");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ChatMessageKey");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ChatMessageKey");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ChatMessageKey");

            migrationBuilder.RenameColumn(
                name: "EncryptedKey",
                table: "ChatMessageDto",
                newName: "EncryptedKeyForSender");

            migrationBuilder.AddColumn<string>(
                name: "EncryptedKeyForReceiver",
                table: "ChatMessageDto",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
