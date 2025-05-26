using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiImageGeneratorApi.Migrations
{
    /// <inheritdoc />
    public partial class upadte_260525 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HinhAnh",
                table: "ChatGroups",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HinhAnh",
                table: "ChatGroupInfoDto",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HinhAnh",
                table: "ChatGroups");

            migrationBuilder.DropColumn(
                name: "HinhAnh",
                table: "ChatGroupInfoDto");
        }
    }
}
