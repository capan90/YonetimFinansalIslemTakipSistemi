namespace YonetimFinansalIslemTakipSistemi.Application.Features.SystemLogs;

public class PagedSystemLogResultDto
{
    public IReadOnlyList<SystemLogListItemDto> Items      { get; init; } = [];
    public int                                 TotalCount { get; init; }
    public int                                 Page       { get; init; }
    public int                                 PageSize   { get; init; }
    public int                                 TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
