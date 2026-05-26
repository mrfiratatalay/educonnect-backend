using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EduConnect.Infrastructure.Extensions;

/// <summary>
/// PostgreSQL unique-violation kontrolu icin helper.
/// Like/Bookmark/Follow/View gibi idempotent insert'lerde race condition'i yutmak icin kullanilir.
/// </summary>
public static class DbUpdateExceptionExtensions
{
    public static bool IsUniqueViolation(this DbUpdateException ex)
    {
        return ex.InnerException is PostgresException pg && pg.SqlState == "23505";
    }
}
