using System.IO.Compression;
using System.Xml;
using ClosedXML.Excel;

// REHBER 1.ods → sihirbaz uyumlu iki xlsx:
//   Firma_Tlf  → rehber-firmalar.xlsx          (Firma Adı | Telefon | Not)
//   Şirket_Tlf → rehber-whatsapp-kisiler.xlsx  (Ad Soyad | Telefon | Açıklama)
var odsPath = @"c:\Users\Murat Capan\Desktop\REHBER 1.ods";
var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

const string TableNs = "urn:oasis:names:tc:opendocument:xmlns:table:1.0";

var sheets = ReadOds(odsPath);

// ── Firma_Tlf → firmalar ──
var firmaRows = sheets["Firma_Tlf"];
var firmalarPath = Path.Combine(desktop, "rehber-firmalar.xlsx");
using (var wb = new XLWorkbook())
{
    var ws = wb.Worksheets.Add("Firma Rehberi");
    WriteHeaders(ws, ["Firma Adı", "Telefon", "Not"]);
    var r = 2;
    foreach (var cells in firmaRows.Skip(1)) // başlık satırını atla
    {
        var name  = Cell(cells, 0);
        if (name is null) continue;
        var phone = Cell(cells, 1);
        var fax   = Cell(cells, 2);

        ws.Cell(r, 1).Value = name;
        if (phone is not null) ws.Cell(r, 2).Value = phone;
        if (fax   is not null) ws.Cell(r, 3).Value = $"Fax: {fax}";
        r++;
    }
    ws.SheetView.FreezeRows(1);
    wb.SaveAs(firmalarPath);
    Console.WriteLine($"{firmalarPath}  ({r - 2} satır)");
}

// ── Şirket_Tlf → WhatsApp kişileri ──
var kisiRows = sheets["Şirket_Tlf"];
var kisilerPath = Path.Combine(desktop, "rehber-whatsapp-kisiler.xlsx");
using (var wb = new XLWorkbook())
{
    var ws = wb.Worksheets.Add("WhatsApp Rehberi");
    WriteHeaders(ws, ["Ad Soyad", "Telefon", "Açıklama"]);
    var r = 2;
    foreach (var cells in kisiRows.Skip(1)) // başlık satırını atla
    {
        var kod   = Cell(cells, 0);
        var phone = Cell(cells, 1);
        var name  = Cell(cells, 2);
        if (name is null && phone is null) continue;

        ws.Cell(r, 1).Value = name ?? "";
        if (phone is not null) ws.Cell(r, 2).Value = phone;
        if (kod   is not null) ws.Cell(r, 3).Value = $"Dahili/Kod: {kod}";
        r++;
    }
    ws.SheetView.FreezeRows(1);
    wb.SaveAs(kisilerPath);
    Console.WriteLine($"{kisilerPath}  ({r - 2} satır)");
}

static string? Cell(List<string> cells, int index)
{
    if (index >= cells.Count) return null;
    var v = string.Join(" ", cells[index].Split(' ', StringSplitOptions.RemoveEmptyEntries));
    return v.Length == 0 ? null : v;
}

static void WriteHeaders(IXLWorksheet ws, string[] headers)
{
    for (var i = 0; i < headers.Length; i++)
    {
        var c = ws.Cell(1, i + 1);
        c.Value = headers[i];
        c.Style.Font.Bold = true;
        c.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCE6F1");
        ws.Column(i + 1).Width = 40;
    }
}

static Dictionary<string, List<List<string>>> ReadOds(string path)
{
    var result = new Dictionary<string, List<List<string>>>();
    using var zip = ZipFile.OpenRead(path);
    using var stream = zip.GetEntry("content.xml")!.Open();
    using var reader = XmlReader.Create(stream, new XmlReaderSettings { IgnoreWhitespace = true });

    List<List<string>>? current = null;
    while (reader.Read())
    {
        if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "table" && reader.NamespaceURI == TableNs)
        {
            current = [];
            result[reader.GetAttribute("name", TableNs) ?? $"Sayfa{result.Count + 1}"] = current;
        }
        else if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "table-row" && reader.NamespaceURI == TableNs && current is not null)
        {
            using var row = reader.ReadSubtree();
            var cells = new List<string>();
            string? pending = null;
            var pendingRepeat = 0;
            while (row.Read())
            {
                if (row.NodeType == XmlNodeType.Element && row.LocalName == "table-cell")
                {
                    if (pending is not null)
                        for (var i = 0; i < Math.Min(pendingRepeat, 30); i++) cells.Add(pending);
                    pendingRepeat = int.TryParse(row.GetAttribute("number-columns-repeated", TableNs), out var rep) ? rep : 1;
                    pending = "";
                }
                else if (row.NodeType == XmlNodeType.Text && pending is not null)
                {
                    pending = pending.Length == 0 ? row.Value : pending + " " + row.Value;
                }
            }
            if (pending is not null)
                for (var i = 0; i < Math.Min(pendingRepeat, 30); i++) cells.Add(pending);

            while (cells.Count > 0 && string.IsNullOrWhiteSpace(cells[^1])) cells.RemoveAt(cells.Count - 1);
            if (cells.Count > 0) current.Add(cells);
        }
    }
    return result;
}
