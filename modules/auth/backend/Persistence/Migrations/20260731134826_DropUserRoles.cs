using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseFramework.Modules.Auth.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropUserRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Roles",
                schema: "auth",
                table: "users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "Roles",
                schema: "auth",
                table: "users",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);
        }
    }
}
