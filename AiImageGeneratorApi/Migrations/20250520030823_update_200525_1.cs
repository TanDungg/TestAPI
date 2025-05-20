using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiImageGeneratorApi.Migrations
{
    /// <inheritdoc />
    public partial class update_200525_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "Menus",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Menus_ParentId",
                table: "Menus",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Menus_Menus_ParentId",
                table: "Menus",
                column: "ParentId",
                principalTable: "Menus",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Menus_Menus_ParentId",
                table: "Menus");

            migrationBuilder.DropIndex(
                name: "IX_Menus_ParentId",
                table: "Menus");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Menus");
        }
    }
}
