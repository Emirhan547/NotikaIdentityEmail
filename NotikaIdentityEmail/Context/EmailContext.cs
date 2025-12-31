using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NotikaIdentityEmail.Entities;

namespace NotikaIdentityEmail.Context
{
    public class EmailContext : IdentityDbContext<AppUser>
    {
         protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;initial Catalog=NotikaEmailDb;integrated security=true;trustServerCertificate=true");
        }
        public DbSet<Category>Categories { get; set; }
        public DbSet<Message>Messages { get; set; }
    }
}
