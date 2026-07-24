using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

// REHBER 2 ŞEHİRLER ARASI.xls → sihirbaz uyumlu xlsx
// Kaynak: Cari adı | Adres | Adres2 | ilçe | İl | Tel Bölge Kodu | Tel No1 | Tel No2
// Hedef:  Firma Adı | Adres | İlçe | İl | Telefon | Not
var src = @"c:\Users\Murat Capan\Desktop\REHBER 2 ŞEHİRLER ARASI.xls";
var dst = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
    "rehber-firmalar-sehirlerarasi.xlsx");
Console.OutputEncoding = Encoding.UTF8;

using var fs = File.OpenRead(src);
var source = new HSSFWorkbook(fs).GetSheetAt(0);

using var wb = new XLWorkbook();
var ws = wb.Worksheets.Add("Firma Rehberi");
string[] headers = ["Firma Adı", "Adres", "İlçe", "İl", "Telefon", "Not"];
for (var i = 0; i < headers.Length; i++)
{
    var c = ws.Cell(1, i + 1);
    c.Value = headers[i];
    c.Style.Font.Bold = true;
    c.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCE6F1");
    ws.Column(i + 1).Width = i is 0 or 1 ? 50 : 20;
}

var outRow = 2;
for (var r = source.FirstRowNum + 1; r <= source.LastRowNum; r++) // başlık satırını atla
{
    var row = source.GetRow(r);
    if (row is null) continue;

    var name = Cell(row, 0);
    if (name is null) continue;

    var adres1 = Cell(row, 1);
    var adres2 = Cell(row, 2);
    var ilce   = Cell(row, 3);
    var il     = Cell(row, 4);
    var kod    = Cell(row, 5);
    var tel1   = Cell(row, 6);
    var tel2   = Cell(row, 7);

    // İki adres = iki ayrı rehber kaydı (bir firmanın farklı adresleri olabilir;
    // kargo gönderisinde doğru adres seçilebilsin). Tel No1 → 1. kayıt, Tel No2 → 2. kayıt.
    string? Phone(string? tel) => tel is null ? null : kod is null ? tel : $"0{kod} {tel}";

    void WriteRow(string? adres, string? phone, string? not)
    {
        ws.Cell(outRow, 1).Value = name;
        if (adres is not null) ws.Cell(outRow, 2).Value = adres;
        if (ilce  is not null) ws.Cell(outRow, 3).Value = ilce;
        if (il    is not null) ws.Cell(outRow, 4).Value = il;
        if (phone is not null) ws.Cell(outRow, 5).Value = phone;
        if (not   is not null) ws.Cell(outRow, 6).Value = not;
        outRow++;
    }

    if (adres1 is null && adres2 is not null) { adres1 = adres2; adres2 = null; }

    if (adres2 is null)
    {
        // Tek adres: her iki numara da bu kayda (2.si Not'a "Ek Tel")
        if (tel1 is null && tel2 is not null) { tel1 = tel2; tel2 = null; }
        WriteRow(adres1, Phone(tel1), tel2 is null ? null : $"Ek Tel: {Phone(tel2)}");
    }
    else
    {
        WriteRow(adres1, Phone(tel1), null);
        WriteRow(adres2, Phone(tel2), "2. adres kaydı");
    }
}

ws.SheetView.FreezeRows(1);
wb.SaveAs(dst);
Console.WriteLine($"{dst}  ({outRow - 2} satır)");

static string? Cell(IRow row, int index)
{
    if (index >= row.LastCellNum) return null;
    var cell = row.GetCell(index);
    if (cell is null) return null;

    var text = cell.CellType switch
    {
        CellType.String  => cell.StringCellValue,
        CellType.Numeric => cell.NumericCellValue == Math.Truncate(cell.NumericCellValue)
            ? ((long)cell.NumericCellValue).ToString()
            : cell.NumericCellValue.ToString(CultureInfo.InvariantCulture),
        CellType.Formula => cell.ToString() ?? "",
        _ => cell.ToString() ?? ""
    };

    var collapsed = string.Join(" ", text.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    return collapsed.Length == 0 ? null : collapsed;
}
