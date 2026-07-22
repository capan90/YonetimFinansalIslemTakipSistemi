using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPrefsWhatsAppDirectoryAndCargoCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PortalUrl",
                table: "cargo_companies",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cargo_number_counters",
                columns: table => new
                {
                    Direction = table.Column<int>(type: "integer", nullable: false),
                    LastValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cargo_number_counters", x => x.Direction);
                });

            migrationBuilder.CreateTable(
                name: "user_preferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TextCase = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
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
                    table.PrimaryKey("PK_user_preferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_preferences_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "whatsapp_contacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_whatsapp_contacts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_preferences_UserId",
                table: "user_preferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_whatsapp_contacts_Phone",
                table: "whatsapp_contacts",
                column: "Phone",
                unique: true);

            // ── Veri migration'ları — mevcut canlı veri korunur, yalnızca ekleme/doldurma yapılır ──

            // 1) Yön başına sayaç satırı (idempotent). Numara üretimi bu satırlar üzerinden
            //    UPDATE ... RETURNING ile atomik yapılır.
            migrationBuilder.Sql("""
                INSERT INTO cargo_number_counters ("Direction", "LastValue")
                VALUES (1, 0), (2, 0)
                ON CONFLICT ("Direction") DO NOTHING;
                """);

            // 2) ShipmentNumber'ı NULL kalmış kayıtlara (soft delete dahil) deterministik backfill:
            //    yön bazında CreatedAt/Id sırasına göre GLN00001/GDN00001 atanır.
            //    Eski format (G-YYYY-NNNN) numaralara DOKUNULMAZ — audit ve basılı etiketler
            //    bu numaralara referans verir.
            migrationBuilder.Sql("""
                WITH numbered AS (
                    SELECT "Id", "Direction",
                           ROW_NUMBER() OVER (PARTITION BY "Direction" ORDER BY "CreatedAt", "Id") AS rn
                    FROM cargo_shipments
                    WHERE "ShipmentNumber" IS NULL
                )
                UPDATE cargo_shipments cs
                SET "ShipmentNumber" =
                    CASE WHEN n."Direction" = 1 THEN 'GLN' ELSE 'GDN' END || LPAD(n.rn::text, 5, '0')
                FROM numbered n
                WHERE cs."Id" = n."Id";
                """);

            // 3) Sayaçları kullanılmış en büyük yeni-format numaraya eşitle — ilk üretim çakışmasız başlar.
            migrationBuilder.Sql("""
                UPDATE cargo_number_counters c
                SET "LastValue" = GREATEST(c."LastValue", COALESCE((
                    SELECT MAX(substring("ShipmentNumber" from 4)::bigint)
                    FROM cargo_shipments
                    WHERE "ShipmentNumber" ~ ('^' || CASE WHEN c."Direction" = 1 THEN 'GLN' ELSE 'GDN' END || '[0-9]+$')
                ), 0));
                """);

            // 4) Yurtiçi Kargo varsayılan portal bağlantısı — kod içinde hard-code edilmez.
            //    Mevcut kayıt varsa yalnızca boş PortalUrl doldurulur; yoksa kontrollü başlangıç
            //    verisi olarak sabit Id ile eklenir (duplicate oluşmaz).
            migrationBuilder.Sql("""
                UPDATE cargo_companies
                SET "PortalUrl" = 'https://selfservis.yurticikargo.com/Login.aspx?ReturnUrl=%2fMain.aspx'
                WHERE ("PortalUrl" IS NULL OR "PortalUrl" = '')
                  AND (lower("Name") LIKE 'yurtiçi%' OR lower("Name") LIKE 'yurtici%');

                INSERT INTO cargo_companies
                    ("Id", "Name", "PortalUrl", "IsActive", "CreatedAt", "IsDeleted")
                SELECT
                    'b7de5a1c-93f4-4f7e-9a51-2f6d1c0a8e42'::uuid,
                    'Yurtiçi Kargo',
                    'https://selfservis.yurticikargo.com/Login.aspx?ReturnUrl=%2fMain.aspx',
                    TRUE, now(), FALSE
                WHERE NOT EXISTS (
                    SELECT 1 FROM cargo_companies
                    WHERE lower("Name") LIKE 'yurtiçi%' OR lower("Name") LIKE 'yurtici%'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cargo_number_counters");

            migrationBuilder.DropTable(
                name: "user_preferences");

            migrationBuilder.DropTable(
                name: "whatsapp_contacts");

            migrationBuilder.DropColumn(
                name: "PortalUrl",
                table: "cargo_companies");
        }
    }
}
