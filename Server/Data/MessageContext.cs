using Microsoft.EntityFrameworkCore;
using NDAProcesses.Shared.Models;

namespace NDAProcesses.Server.Data
{
    public class MessageContext : DbContext
    {
        public MessageContext(DbContextOptions<MessageContext> options) : base(options)
        {
        }

        public DbSet<MessageModel> Messages => Set<MessageModel>();
        public DbSet<ContactModel> Contacts => Set<ContactModel>();
        public DbSet<ScheduledMessageModel> ScheduledMessages => Set<ScheduledMessageModel>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<MessageModel>()
                .HasIndex(m => m.ExternalId)
                .IsUnique();
        }
    }
}
