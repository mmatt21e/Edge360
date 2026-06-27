using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edge360.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "OfflineNotified",
                table: "devices",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OfflineNotified",
                table: "devices");
        }
    }
}
