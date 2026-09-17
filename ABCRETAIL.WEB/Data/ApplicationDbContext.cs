using ABCRETAIL.WEB.Models;
using Microsoft.EntityFrameworkCore;

namespace ABCRETAIL.WEB.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<CustomerProfile> Customers { get; set; }

        public DbSet<OrderMessage> OrderMessages { get; set; }

        public DbSet<Product> Products { get; set; }
    }
}
