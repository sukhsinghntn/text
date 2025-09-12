using Microsoft.EntityFrameworkCore;
using NDAProcesses.Shared.Models;

namespace NDAProcesses.Server.Data
{
    public class MessageContext : DbContext
    {
        public MessageContext(DbContextOptions<MessageContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<MessageModel> Messages => Set<MessageModel>();
        public DbSet<ContactModel> Contacts => Set<ContactModel>();
        public DbSet<ScheduledMessageModel> ScheduledMessages => Set<ScheduledMessageModel>();
        public DbSet<ReadStateModel> ReadStates => Set<ReadStateModel>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<MessageModel>()
                .HasIndex(m => m.ExternalId)
                .IsUnique();

            modelBuilder.Entity<ReadStateModel>()
                .HasIndex(r => new { r.Department, r.Recipient })
                .IsUnique();
        }
    }
}
