using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace PostgresPooling
{
    internal partial class Program
    {
        private static readonly int iterations = 50;
        private static readonly Guid id = Guid.NewGuid();
        private static readonly bool inParallel = false; // change this

        public static async Task Main()
        {
            Console.WriteLine($"Running test {(inParallel ? "in parallel" : "sequentially")}");
            var services = new ServiceCollection();
            services.AddDbContext<TestDbContext>((serviceProvider, options) =>
            {
                var connectionString = GetConnectionStringBuilder().ConnectionString;
                options.UseNpgsql(connectionString, pgOptions =>
                {
                    pgOptions.MigrationsAssembly(typeof(TestDbContext).Assembly.GetName().FullName);
                    pgOptions.MigrationsHistoryTable("__EFMigrationsHistory", $"Db_{id}");
                    pgOptions.EnableRetryOnFailure();
                });
            });

            using var provider = services.BuildServiceProvider();
            await CreateDbAsync(provider).ConfigureAwait(false);
            try
            {
                await RunTasksAsync(provider).ConfigureAwait(false);
                await CheckConnectionsAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                await FinishAsync(provider).ConfigureAwait(false);
            }
            Console.Read();
        }

        private static async Task CreateDbAsync(IServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await dbContext.Database.MigrateAsync().ConfigureAwait(false); // creates db implicitly
        }

        private static async Task RunTasksAsync(IServiceProvider provider)
        {
            try
            {
                if (inParallel)
                {
                    var tasks = Enumerable.Range(0, iterations).Select(_ => QueryDbAsync(provider));
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                else
                {
                    for (int i = 0; i < iterations; i++)
                        await QueryDbAsync(provider).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static async Task QueryDbAsync(IServiceProvider provider)
        {
            using var scope = provider.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();

            await dbContext.Foos.AddAsync(new Foo()).ConfigureAwait(true);
            await dbContext.SaveChangesAsync().ConfigureAwait(true);

            await dbContext.Foos.AddAsync(new Foo()).ConfigureAwait(true);
            await dbContext.SaveChangesAsync().ConfigureAwait(true);
        }

        private static async Task CheckConnectionsAsync()
        {
            var connStringBuilder = GetConnectionStringBuilder();
            connStringBuilder["Database"] = "postgres";
            using var conn = new Npgsql.NpgsqlConnection(connStringBuilder.ConnectionString);
            try
            {
                conn.Open();
                var command = conn.CreateCommand();
                command.CommandText = "SELECT application_name, state, datname, COUNT(application_name) AS count " +
                    "FROM pg_catalog.pg_stat_activity " +
                    "WHERE application_name LIKE 'test' " +
                    "GROUP BY application_name, state, datname";
                using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
                var read = false;
                do
                {
                    read = await reader.ReadAsync().ConfigureAwait(false);
                    if (reader.IsOnRow)
                        Console.WriteLine($"{reader.GetValue("state")} connections for {reader.GetValue("datname")}: {reader.GetValue("count")}");
                }
                while (read);
            }
            finally { conn.Close(); }
        }

        private static async Task FinishAsync(IServiceProvider provider)
        {
            using var scope = provider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var fooCount = await dbContext.Foos.CountAsync();
            Console.WriteLine($"Created {fooCount} things.");

            await dbContext.Database.EnsureDeletedAsync().ConfigureAwait(false);
        }

        public static DbConnectionStringBuilder GetConnectionStringBuilder()
        {
            return new DbConnectionStringBuilder
            {
                ["Host"] = "localhost",
                ["Username"] = "postgres",
                ["Password"] = "postgres",
                ["Database"] = $"TestDb-{id}",
                ["Port"] = "5432",
                ["Application Name"] = "test",
                ["Pooling"] = "True",
                ["Minimum Pool Size"] = 0,
                ["Maximum Pool Size"] = 100,
                ["Connection Lifetime"] = 0, // disabled 
                ["Connection Idle Lifetime"] = 30,
                ["Include Error Detail"] = "True"
            };
        }
    }
}