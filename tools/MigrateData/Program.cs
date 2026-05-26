using System.Reflection;
using EduConnect.Infrastructure.Data;
using MigrateData;
using Microsoft.EntityFrameworkCore;

const string DefaultSqlServerConnectionString =
    @"Server=localhost\SQLEXPRESS;Database=EduConnectDb;Trusted_Connection=True;TrustServerCertificate=True;";
const string DefaultPostgresConnectionString =
    "Host=localhost;Port=5433;Database=educonnect;Username=educonnect;Password=educonnect_dev";

var sqlServerConnectionString =
    Environment.GetEnvironmentVariable("MIGRATEDATA_SOURCE") ?? DefaultSqlServerConnectionString;
var postgresConnectionString =
    Environment.GetEnvironmentVariable("MIGRATEDATA_TARGET") ?? DefaultPostgresConnectionString;

Console.WriteLine("EduConnect veri gocu basliyor.");
Console.WriteLine($"  Kaynak  (MS SQL)    : {Mask(sqlServerConnectionString)}");
Console.WriteLine($"  Hedef   (PostgreSQL): {Mask(postgresConnectionString)}");
Console.WriteLine();

await using var sourceCtx = BuildSource(sqlServerConnectionString);
await using var targetCtx = BuildTarget(postgresConnectionString);

if (!await sourceCtx.Database.CanConnectAsync())
{
    Console.Error.WriteLine("HATA: MS SQL Server'a baglanilamadi. EduConnectDb restore edilmis mi?");
    return 1;
}

Console.WriteLine("PostgreSQL semasinin hazir oldugundan emin olunuyor (migration apply)...");
await targetCtx.Database.MigrateAsync();

// Connection'i acik tut ki session_replication_role tum SaveChanges'lar boyunca aktif kalsin.
// Aksi halde EF Core her batch'te pool'dan yeni connection alabilir ve setting reset olur.
await targetCtx.Database.OpenConnectionAsync();

Console.WriteLine("PostgreSQL'de FK constraint trigger'lari gecici devre disi biraktiriliyor.");
await targetCtx.Database.ExecuteSqlRawAsync("SET session_replication_role = replica;");

var migrationOrder = ResolveMigrationOrder(sourceCtx);

Console.WriteLine();
Console.WriteLine($"{migrationOrder.Count} tablo bulundu. Kopyalama basliyor.");
Console.WriteLine();

var migrateMethod = typeof(Migrator).GetMethod(nameof(Migrator.MigrateTableAsync),
    BindingFlags.Public | BindingFlags.Static)!;

var totalRows = 0L;
foreach (var entityType in migrationOrder)
{
    var clrType = entityType.ClrType;
    var generic = migrateMethod.MakeGenericMethod(clrType);
    var task = (Task<int>)generic.Invoke(null, new object[] { sourceCtx, targetCtx })!;
    var inserted = await task;
    totalRows += inserted;
    Console.WriteLine($"  + {clrType.Name,-35} {inserted,8} satir");
}

Console.WriteLine();
Console.WriteLine("FK constraint trigger'lari yeniden aktif ediliyor.");
await targetCtx.Database.ExecuteSqlRawAsync("SET session_replication_role = origin;");
await targetCtx.Database.CloseConnectionAsync();

Console.WriteLine();
Console.WriteLine($"BITTI. Toplam {totalRows} satir kopyalandi.");
return 0;

static AppDbContext BuildSource(string connectionString)
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlServer(connectionString)
        .Options;
    return new AppDbContext(options);
}

static AppDbContext BuildTarget(string connectionString)
{
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(connectionString)
        .Options;
    return new AppDbContext(options);
}

static List<Microsoft.EntityFrameworkCore.Metadata.IEntityType> ResolveMigrationOrder(AppDbContext ctx)
{
    // FK'lar devre disi oldugu icin sira kritik degil, yine de deterministik olsun diye
    // dependency'si az olan entity'leri once isle.
    var all = ctx.Model.GetEntityTypes().Where(t => !t.IsOwned()).ToList();
    return all.OrderBy(t => t.GetForeignKeys().Count()).ThenBy(t => t.ClrType.Name).ToList();
}

static string Mask(string connectionString)
{
    var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
    return string.Join(';', parts.Select(p =>
        p.Trim().StartsWith("Password", StringComparison.OrdinalIgnoreCase)
            ? "Password=***"
            : p));
}
