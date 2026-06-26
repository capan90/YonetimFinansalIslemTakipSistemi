using YonetimFinansalIslemTakipSistemi.Application.Features.CargoShipment.Queries.GetCargoReport;

namespace YonetimFinansalIslemTakipSistemi.Application.Interfaces.Services;

public interface ICargoReportPdfExporter
{
    byte[] Export(CargoReportDto report);
}
