using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiImageGeneratorApi.Migrations
{
    /// <inheritdoc />
    public partial class update_270525 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessageReads_ChatMessages_MessageId",
                table: "ChatMessageReads");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessageReads_Users_UserId",
                table: "ChatMessageReads");

            migrationBuilder.DropTable(
                name: "ChatGroupInfoDto");

            migrationBuilder.DropTable(
                name: "ChatUserInfoDto");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessageReads_MessageId",
                table: "ChatMessageReads");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessageReads_UserId",
                table: "ChatMessageReads");

            migrationBuilder.DropColumn(
                name: "MessageId",
                table: "ChatMessageReads");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ChatMessageReads");

            migrationBuilder.CreateTable(
                name: "ChatInfoMessage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HinhAnh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsNhom = table.Column<bool>(type: "bit", nullable: false),
                    SoLuongThanhVien = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    List_Ngays = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessageReads_ThanhVienId",
                table: "ChatMessageReads",
                column: "ThanhVienId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessageReads_TinNhanId",
                table: "ChatMessageReads",
                column: "TinNhanId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessageReads_ChatMessages_TinNhanId",
                table: "ChatMessageReads",
                column: "TinNhanId",
                principalTable: "ChatMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessageReads_Users_ThanhVienId",
                table: "ChatMessageReads",
                column: "ThanhVienId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessageReads_ChatMessages_TinNhanId",
                table: "ChatMessageReads");

            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessageReads_Users_ThanhVienId",
                table: "ChatMessageReads");

            migrationBuilder.DropTable(
                name: "ChatInfoMessage");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessageReads_ThanhVienId",
                table: "ChatMessageReads");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessageReads_TinNhanId",
                table: "ChatMessageReads");

            migrationBuilder.AddColumn<Guid>(
                name: "MessageId",
                table: "ChatMessageReads",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "ChatMessageReads",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ChatGroupInfoDto",
                columns: table => new
                {
                    HinhAnh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    List_Ngays = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NhomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenNhom = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "ChatUserInfoDto",
                columns: table => new
                {
                    DiaChi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HinhAnh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HoVaTen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    List_Ngays = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NguoiNhanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sdt = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessageReads_MessageId",
                table: "ChatMessageReads",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessageReads_UserId",
                table: "ChatMessageReads",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessageReads_ChatMessages_MessageId",
                table: "ChatMessageReads",
                column: "MessageId",
                principalTable: "ChatMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessageReads_Users_UserId",
                table: "ChatMessageReads",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
