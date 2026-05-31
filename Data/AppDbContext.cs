using AutomationBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace AutomationBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ServiceItem> Services { get; set; }
        public DbSet<DownloadItem> Downloads { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<UploadedFile> UploadedFiles { get; set; }
    }
}
