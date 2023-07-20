using Microsoft.EntityFrameworkCore;

namespace PostgresPooling
{
    public class TestDbContext : DbContext
    {
        public DbSet<Foo> Foos => Set<Foo>();

        public TestDbContext(DbContextOptions options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Foo>().HasKey(x => x.Id);
            modelBuilder.Entity<Foo>().Property(x => x.Index);
        }
    }
}