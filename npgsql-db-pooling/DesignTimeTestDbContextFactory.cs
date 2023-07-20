using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PostgresPooling
{
    public class DesignTimeTestDbContextFactory : IDesignTimeDbContextFactory<TestDbContext>
    {
        public TestDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<TestDbContext>();
            builder.UseNpgsql();
            return new TestDbContext(builder.Options);
        }
    }
}