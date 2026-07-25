using System.Globalization;
using System.Xml.Linq;
using YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;
using YonetimFinansalIslemTakipSistemi.Domain.Enums;

namespace YonetimFinansalIslemTakipSistemi.Infrastructure.Services;

/// <summary>
/// TCMB'nin ücretsiz, anahtarsız günlük kur XML servisinden USD/EUR döviz kurlarını çeker.
///   - Bugün:   https://www.tcmb.gov.tr/kurlar/today.xml
///   - Geçmiş:  https://www.tcmb.gov.tr/kurlar/YYYYMM/DDMMYYYY.xml
/// Hafta sonu/resmi tatilde ilgili gün için dosya yayınlanmaz (404). Bu durumda
/// bir önceki iş gününe düşülür (en fazla 7 gün geriye) — TCMB kuralınca son iş
/// gününün kuru o günler için geçerli sayılır.
/// XML değerlerinde ondalık ayırıcı NOKTA'dır (ör. 47.1647) → InvariantCulture ile okunur.
/// </summary>
public sealed class TcmbExchangeRateSource : IExchangeRateSource
{
    private const string BaseUrl = "https://www.tcmb.gov.tr/kurlar/";
    private const int MaxFallbackDays = 7;

    private readonly HttpClient _httpClient;

    public TcmbExchangeRateSource(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<ExchangeRateSourceData>> FetchAsync(DateTime date)
    {
        // İstenen günden başlayıp geriye doğru ilk yayınlanmış (200 + USD/EUR içeren) dosyayı bul.
        for (var offset = 0; offset <= MaxFallbackDays; offset++)
        {
            var day = date.Date.AddDays(-offset);
            var url = BuildUrl(day);

            string xml;
            try
            {
                using var response = await _httpClient.GetAsync(url);
                // 404 = o gün için yayın yok (hafta sonu/tatil) → bir önceki güne düş
                if (!response.IsSuccessStatusCode) continue;
                xml = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException)
            {
                // Ağ hatası: bir sonraki güne düşmek yerine dışarıya bildir (bağlantı sorunu)
                throw;
            }
            catch (TaskCanceledException)
            {
                throw; // timeout
            }

            var rates = ParseRates(xml);
            if (rates.Count > 0) return rates;
        }

        // 7 iş günü geriye gidildi, hiç kur bulunamadı — çağıran anlaşılır mesaj gösterir
        return [];
    }

    /// <summary>Verilen gün için TCMB kur dosyasının URL'sini üretir (bugün → today.xml).</summary>
    private static string BuildUrl(DateTime day)
    {
        if (day.Date == DateTime.Today)
            return $"{BaseUrl}today.xml";

        // Klasör: YYYYMM, dosya: DDMMYYYY.xml
        return $"{BaseUrl}{day:yyyyMM}/{day:ddMMyyyy}.xml";
    }

    /// <summary>
    /// TCMB kur XML'ini USD ve EUR için ayrıştırır. Ağdan bağımsızdır (test edilebilir).
    /// Yalnızca ForexBuying ve ForexSelling'i dolu olan para birimleri döner.
    /// </summary>
    public static IReadOnlyList<ExchangeRateSourceData> ParseRates(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return [];

        XDocument doc;
        try
        {
            doc = XDocument.Parse(xml);
        }
        catch (System.Xml.XmlException)
        {
            return []; // bozuk/HTML yanıt (ör. hata sayfası) → boş
        }

        var result = new List<ExchangeRateSourceData>();

        foreach (var currency in doc.Descendants("Currency"))
        {
            var code = (string?)currency.Attribute("Kod")
                    ?? (string?)currency.Attribute("CurrencyCode");

            var mapped = code switch
            {
                "USD" => (CurrencyType?)CurrencyType.USD,
                "EUR" => CurrencyType.EUR,
                _     => null
            };
            if (mapped is null) continue;

            if (!TryParseDecimal(currency.Element("ForexBuying")?.Value, out var buying)) continue;
            if (!TryParseDecimal(currency.Element("ForexSelling")?.Value, out var selling)) continue;

            result.Add(new ExchangeRateSourceData(mapped.Value, buying, selling));
        }

        return result;
    }

    private static bool TryParseDecimal(string? raw, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        // TCMB ondalık ayırıcısı nokta → InvariantCulture
        return decimal.TryParse(raw.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out value)
               && value > 0;
    }
}
