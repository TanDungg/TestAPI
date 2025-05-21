using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiImageGeneratorApi.Migrations
{
    /// <inheritdoc />
    public partial class update210525 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsThongBao",
                table: "ChatMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LoaiThongBao",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "ChatMessageDto",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsThongBao",
                table: "ChatMessageDto",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LoaiThongBao",
                table: "ChatMessageDto",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChatUserInfoDto",
                columns: table => new
                {
                    NguoiNhanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HoVaTen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiaChi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sdt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HinhAnh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    list_Messages = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatUserInfoDto");

            migrationBuilder.DropColumn(
                name: "IsThongBao",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "LoaiThongBao",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "ChatMessageDto");

            migrationBuilder.DropColumn(
                name: "IsThongBao",
                table: "ChatMessageDto");

            migrationBuilder.DropColumn(
                name: "LoaiThongBao",
                table: "ChatMessageDto");
        }
    }
}
