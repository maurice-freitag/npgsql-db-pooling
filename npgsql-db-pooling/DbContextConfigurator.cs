using Microsoft.EntityFrameworkCore;

namespace PostgresPooling
{
    public class DbContextConfigurator
    {
        private readonly Action<string, string, DbContextOptionsBuilder> configureAction;

        public DbContextConfigurator(Action<string, string, DbContextOptionsBuilder> configureAction)
        {
            this.configureAction = configureAction ?? throw new ArgumentNullException(nameof(configureAction));
        }

        public void Configure(
            string connectionString,
            string dbSchemaName,
            DbContextOptionsBuilder options) => configureAction.Invoke(connectionString, dbSchemaName, options);
    }
}