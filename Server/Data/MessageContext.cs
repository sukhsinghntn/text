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
    }
}
