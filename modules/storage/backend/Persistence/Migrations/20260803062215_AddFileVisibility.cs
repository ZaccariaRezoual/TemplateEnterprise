using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseFramework.Modules.Storage.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFileVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The defaults are written by hand, not left as the scaffolded
            // empty string. Rows uploaded before this migration were never
            // sniffed, so the honest value is "opaque bytes".
            migrationBuilder.AddColumn<string>(
                name: "SafeContentType",
                schema: "storage",
                table: "files",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "application/octet-stream");

            // Private for everything that already exists: nobody consented to
            // publishing files uploaded before the concept existed. An empty
            // string here would also fail to map back to the enum — at read
            // time, on a random request, rather than during the migration.
            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                schema: "storage",
                table: "files",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Private");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SafeContentType",
                schema: "storage",
                table: "files");

            migrationBuilder.DropColumn(
                name: "Visibility",
                schema: "storage",
                table: "files");
        }
    }
}
