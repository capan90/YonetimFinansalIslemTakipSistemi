using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorTransactionTypesAndGridLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_grid_layouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScreenKey = table.Column<string>(type: "text", nullable: false),
                    LayoutJson = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_grid_layouts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_grid_layouts_UserId_ScreenKey",
                table: "user_grid_layouts",
                columns: new[] { "UserId", "ScreenKey" },
                unique: true);

            // Veri migrasyonu: Ödeme(2), Avans(3), ÖzelHarcama(4), Transfer(5) → Çıkış(2)
            // Tahsilat(1) zaten Giriş(1) ile aynı değeri taşıdığından güncelleme gerekmez.
            migrationBuilder.Sql(
                     "UPDATE cash_transactions SET \"TransactionType\" = 2 WHERE \"TransactionType\" IN (2, 3, 4, 5);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_grid_layouts");
        }
    }
}
