using Microsoft.EntityFrameworkCore;
using NDAProcesses.Shared.Models;

namespace NDAProcesses.Server.Data
{
    public class MessagingContext : DbContext
    {
        public MessagingContext(DbContextOptions<MessagingContext> options) : base(options)
        {
        }

        public DbSet<MessageModel> Messages => Set<MessageModel>();
    }
}
