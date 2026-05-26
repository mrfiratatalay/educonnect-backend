using EduConnect.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MigrateData;

public static class Migrator
{
    public static async Task<int> MigrateTableAsync<TEntity>(AppDbContext source, AppDbContext target)
        where TEntity : class
    {
        var items = await source.Set<TEntity>().AsNoTracking().ToListAsync();
        if (items.Count == 0)
        {
            return 0;
        }

        await target.Set<TEntity>().AddRangeAsync(items);
        await target.SaveChangesAsync();
        foreach (var entry in target.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }
        return items.Count;
    }
}
