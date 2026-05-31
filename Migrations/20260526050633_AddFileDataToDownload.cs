using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutomationBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddFileDataToDownload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "FileData",
                table: "Downloads",
                type: "varbinary(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileData",
                table: "Downloads");
        }
    }
}
