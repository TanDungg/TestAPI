using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiImageGeneratorApi.Migrations
{
    /// <inheritdoc />
    public partial class update110625 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatMessageKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TinNhanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ThanhVienId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EncryptedKey = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessageKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessageKeys_ChatMessages_TinNhanId",
                        column: x => x.TinNhanId,
                        principalTable: "ChatMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessageKeys_Users_ThanhVienId",
                        column: x => x.ThanhVienId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessageKeys_ThanhVienId",
                table: "ChatMessageKeys",
                column: "ThanhVienId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessageKeys_TinNhanId",
                table: "ChatMessageKeys",
                column: "TinNhanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessageKeys");
        }
    }
}
