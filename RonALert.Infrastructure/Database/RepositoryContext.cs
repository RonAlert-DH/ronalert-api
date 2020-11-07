using Microsoft.EntityFrameworkCore;
using RonALert.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RonALert.Infrastructure.Database
{
    public class RepositoryContext : DbContext
    {
        public DbSet<Alarm> Alarms { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<PersonPosition> PersonPositions { get; set; }

        public RepositoryContext(DbContextOptions<RepositoryContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder) { }

        public override int SaveChanges()
        {
            AddTimestamps();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            AddTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void AddTimestamps()
        {
            var entities = ChangeTracker.Entries()
                .Where(x => x.Entity is EntityBase && (x.State == EntityState.Added || x.State == EntityState.Modified));

            foreach (var entity in entities)
            {
                var now = DateTime.UtcNow;

                if (entity.State == EntityState.Added)
                    ((EntityBase)entity.Entity).CreatedAtUtc = now;

                ((EntityBase)entity.Entity).UpdatedAtUtc = now;
            }
        }
    }
}
