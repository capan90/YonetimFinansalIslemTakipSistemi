using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoShipmentWorkflowFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Operasyonel öncelik; varsayılan Normal (1)
            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "cargo_shipments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // Kayıt oluşturma kanalı; varsayılan Manual (1)
            migrationBuilder.AddColumn<int>(
                name: "CreatedFrom",
                table: "cargo_shipments",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            // Alıcı firma snapshot alanları — kargo oluşturulurken CompanyDirectory'den kopyalanır
            migrationBuilder.AddColumn<string>(
                name: "ReceiverCompanyNameSnapshot",
                table: "cargo_shipments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverAddressSnapshot",
                table: "cargo_shipments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverAttentionSnapshot",
                table: "cargo_shipments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverCitySnapshot",
                table: "cargo_shipments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverDistrictSnapshot",
                table: "cargo_shipments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverPhoneSnapshot",
                table: "cargo_shipments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverEmailSnapshot",
                table: "cargo_shipments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Priority",                    table: "cargo_shipments");
            migrationBuilder.DropColumn(name: "CreatedFrom",                 table: "cargo_shipments");
            migrationBuilder.DropColumn(name: "ReceiverCompanyNameSnapshot", table: "cargo_shipments");
            migrationBuilder.DropColumn(name: "ReceiverAddressSnapshot",     table: "cargo_shipments");
            migrationBuilder.DropColumn(name: "ReceiverAttentionSnapshot",   table: "cargo_shipments");
            migrationBuilder.DropColumn(name: "ReceiverCitySnapshot",        table: "cargo_shipments");
            migrationBuilder.DropColumn(name: "ReceiverDistrictSnapshot",    table: "cargo_shipments");
            migrationBuilder.DropColumn(name: "ReceiverPhoneSnapshot",       table: "cargo_shipments");
            migrationBuilder.DropColumn(name: "ReceiverEmailSnapshot",       table: "cargo_shipments");
        }
    }
}
