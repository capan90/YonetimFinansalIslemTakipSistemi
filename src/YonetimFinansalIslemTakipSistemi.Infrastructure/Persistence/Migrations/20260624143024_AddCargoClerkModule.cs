using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoClerkModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cargo_companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TrackingUrlTemplate = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Website = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cargo_companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "company_directories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactPerson = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AttentionTo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AddressLine = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    District = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company_directories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cargo_shipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    ShipmentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ShipmentTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ShipmentType = table.Column<int>(type: "integer", nullable: true),
                    CargoCompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompanyDirectoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    SenderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReceiverName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeliveredBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReceivedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    VehiclePlate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TrackingNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TrackingUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    NotificationStatus = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cargo_shipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cargo_shipments_cargo_companies_CargoCompanyId",
                        column: x => x.CargoCompanyId,
                        principalTable: "cargo_companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_cargo_shipments_company_directories_CompanyDirectoryId",
                        column: x => x.CompanyDirectoryId,
                        principalTable: "company_directories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cargo_shipments_CargoCompanyId",
                table: "cargo_shipments",
                column: "CargoCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_cargo_shipments_CompanyDirectoryId",
                table: "cargo_shipments",
                column: "CompanyDirectoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cargo_shipments");

            migrationBuilder.DropTable(
                name: "cargo_companies");

            migrationBuilder.DropTable(
                name: "company_directories");
        }
    }
}
