using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoShipmentOperationalFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mevcut kayıtlarda ShipmentNumber boşsa yön ve yıla göre otomatik doldurulur.
            // Zaten numarası olan kayıtlara dokunulmaz; sequence onların maksimumundan devam eder.
            migrationBuilder.Sql(@"
WITH existing_max AS (
    SELECT
        ""Direction"",
        EXTRACT(YEAR FROM ""ShipmentDate"")::int AS yr,
        MAX(CASE
            WHEN ""ShipmentNumber"" ~ '^[CG]-[0-9]{4}-[0-9]+$'
            THEN CAST(SPLIT_PART(""ShipmentNumber"", '-', 3) AS int)
            ELSE 0
        END) AS max_seq
    FROM cargo_shipments
    WHERE ""ShipmentNumber"" IS NOT NULL
    GROUP BY ""Direction"", EXTRACT(YEAR FROM ""ShipmentDate"")::int
),
null_rows AS (
    SELECT
        cs.""Id"",
        cs.""Direction"",
        EXTRACT(YEAR FROM cs.""ShipmentDate"")::int AS yr,
        COALESCE(em.max_seq, 0) + ROW_NUMBER() OVER (
            PARTITION BY cs.""Direction"", EXTRACT(YEAR FROM cs.""ShipmentDate"")::int
            ORDER BY cs.""ShipmentDate"", cs.""CreatedAt""
        ) AS seq
    FROM cargo_shipments cs
    LEFT JOIN existing_max em
        ON em.""Direction"" = cs.""Direction""
        AND em.yr = EXTRACT(YEAR FROM cs.""ShipmentDate"")::int
    WHERE cs.""ShipmentNumber"" IS NULL
)
UPDATE cargo_shipments cs
SET ""ShipmentNumber"" = CASE nr.""Direction""
    WHEN 1 THEN 'C-' || nr.yr || '-' || LPAD(nr.seq::text, 4, '0')
    WHEN 2 THEN 'G-' || nr.yr || '-' || LPAD(nr.seq::text, 4, '0')
END
FROM null_rows nr
WHERE cs.""Id"" = nr.""Id"";
");

            // ShipmentNumber benzersizlik kısıtlaması: null değerler kısıtlamadan muaf
            migrationBuilder.CreateIndex(
                name: "IX_cargo_shipments_ShipmentNumber",
                table: "cargo_shipments",
                column: "ShipmentNumber",
                unique: true,
                filter: "\"ShipmentNumber\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cargo_shipments_ShipmentNumber",
                table: "cargo_shipments");
        }
    }
}
