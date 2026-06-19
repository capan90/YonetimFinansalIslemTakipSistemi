using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExchangeRatesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exchange_rates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    rate_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    currency_type = table.Column<int>(type: "integer", nullable: false),
                    forex_buying = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    forex_selling = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exchange_rates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_exchange_rates_date_currency",
                table: "exchange_rates",
                columns: new[] { "rate_date", "currency_type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exchange_rates");
        }
    }
}
