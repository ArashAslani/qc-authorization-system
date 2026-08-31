namespace AccessManagement.Application.Common.Models;

public record PaginatedList<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 200;

    public static (int PageNumber, int PageSize) Normalize(int pageNumber, int pageSize)
    {
        var page = Math.Max(1, pageNumber);
        var size = Math.Clamp(pageSize, 1, MaxPageSize);
        return (page, size);
    }
}
