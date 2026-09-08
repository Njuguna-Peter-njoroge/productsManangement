using Microsoft.EntityFrameworkCore;
using productsManangement.Models;

namespace productsManangement.Data
{
    public class appDbContext(DbContextOptions<appDbContext> options) : DbContext(options )
    {
        public DbSet<Product> Products {  get; set; }


    }
}
