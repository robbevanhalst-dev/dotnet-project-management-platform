using Microsoft.EntityFrameworkCore;

namespace ProjectManagement.Application.Common;

public static class PaginationExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var count = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, count, pageNumber, pageSize);
    }

    public static PagedResult<T> ToPagedResult<T>(
        this IEnumerable<T> source,
        int pageNumber,
        int pageSize)
    {
        return PagedResult<T>.Create(source, pageNumber, pageSize);
    }
}
