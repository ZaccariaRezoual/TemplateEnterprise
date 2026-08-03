using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseFramework.Modules.Appointments.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAppointmentsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "appointments");

            migrationBuilder.CreateTable(
                name: "appointment_reminders",
                schema: "appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointment_reminders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "appointments",
                schema: "appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ContactPhone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CustomerNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AdminNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "availability_exceptions",
                schema: "appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DateLocal = table.Column<DateOnly>(type: "date", nullable: false),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    StartLocal = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EndLocal = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_availability_exceptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "availability_rules",
                schema: "appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartLocal = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndLocal = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_availability_rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customer_projections",
                schema: "appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LastContactPhone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_projections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "service_projections",
                schema: "appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    IsBookable = table.Column<bool>(type: "boolean", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_projections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "appointment_history",
                schema: "appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FromStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ToStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointment_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_appointment_history_appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalSchema: "appointments",
                        principalTable: "appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_appointment_history_AppointmentId_AtUtc",
                schema: "appointments",
                table: "appointment_history",
                columns: new[] { "AppointmentId", "AtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_appointment_reminders_AppointmentId_Kind",
                schema: "appointments",
                table: "appointment_reminders",
                columns: new[] { "AppointmentId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_appointments_CustomerUserId_StartUtc",
                schema: "appointments",
                table: "appointments",
                columns: new[] { "CustomerUserId", "StartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_ServiceId",
                schema: "appointments",
                table: "appointments",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_StartUtc_Status",
                schema: "appointments",
                table: "appointments",
                columns: new[] { "StartUtc", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_availability_exceptions_DateLocal",
                schema: "appointments",
                table: "availability_exceptions",
                column: "DateLocal");

            migrationBuilder.CreateIndex(
                name: "IX_availability_rules_DayOfWeek",
                schema: "appointments",
                table: "availability_rules",
                column: "DayOfWeek");

            migrationBuilder.CreateIndex(
                name: "IX_service_projections_Slug",
                schema: "appointments",
                table: "service_projections",
                column: "Slug");

            // ---------------------------------------------------------------
            // The exclusion constraint: written by hand because EF Core cannot
            // model one, and because it is the single most important line of
            // this module.
            //
            // Two people asking for the same hour is normal. Two people HAVING
            // it is a broken business. A "is anything booked then?" check
            // followed by an INSERT does not prevent that: between the read
            // and the write there is a gap, and two requests fit through it
            // comfortably. Only the database can close it.
            //
            // Restricted to Confirmed on purpose. A request reserves nothing —
            // that is the module's whole concurrency design — so several may
            // overlap freely, and whoever administers chooses which becomes
            // real. Confirming the second one is then refused HERE, and the
            // handler turns that refusal into a sentence a human can act on.
            //
            // The range is half-open, [start, end): an appointment ending at
            // 10:00 does not collide with one starting at 10:00, which is what
            // "back to back" means.
            //
            // btree_gist is deliberately NOT required: the constraint has one
            // gist column (the range) and no scalar one. The day this becomes
            // per-operator, the operator column joins it WITH = — and that is
            // when the extension will be needed.
            // ---------------------------------------------------------------
            migrationBuilder.Sql(
                """
                ALTER TABLE appointments.appointments
                  ADD CONSTRAINT appointments_no_overlap
                  EXCLUDE USING gist (
                    tstzrange("StartUtc", "EndUtc", '[)') WITH &&
                  ) WHERE ("Status" = 'Confirmed');
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Dropped first: the table it guards is dropped below, and a
            // rollback that leaves a constraint behind is a rollback that
            // cannot be re-applied.
            migrationBuilder.Sql(
                "ALTER TABLE appointments.appointments DROP CONSTRAINT IF EXISTS appointments_no_overlap;"
            );

            migrationBuilder.DropTable(
                name: "appointment_history",
                schema: "appointments");

            migrationBuilder.DropTable(
                name: "appointment_reminders",
                schema: "appointments");

            migrationBuilder.DropTable(
                name: "availability_exceptions",
                schema: "appointments");

            migrationBuilder.DropTable(
                name: "availability_rules",
                schema: "appointments");

            migrationBuilder.DropTable(
                name: "customer_projections",
                schema: "appointments");

            migrationBuilder.DropTable(
                name: "service_projections",
                schema: "appointments");

            migrationBuilder.DropTable(
                name: "appointments",
                schema: "appointments");
        }
    }
}
