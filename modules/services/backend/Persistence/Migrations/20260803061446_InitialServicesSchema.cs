using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseFramework.Modules.Services.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialServicesSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "services");

            migrationBuilder.CreateTable(
                name: "services",
                schema: "services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ShortDescription = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    IsBookable = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_services", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "service_images",
                schema: "services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    AltText = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsCover = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_service_images_services_ServiceId",
                        column: x => x.ServiceId,
                        principalSchema: "services",
                        principalTable: "services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_service_images_ServiceId_SortOrder",
                schema: "services",
                table: "service_images",
                columns: new[] { "ServiceId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_services_IsPublished_IsArchived_SortOrder",
                schema: "services",
                table: "services",
                columns: new[] { "IsPublished", "IsArchived", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_services_Slug",
                schema: "services",
                table: "services",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_images",
                schema: "services");

            migrationBuilder.DropTable(
                name: "services",
                schema: "services");
        }
    }
}
