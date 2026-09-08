using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using productsManangement.Data;
using productsManangement.Dtos;
using productsManangement.Models;

namespace productsManangement.Services
{
    public class productService : IProductService
    {
        private readonly appDbContext context;
        private readonly ILogger<productService> _logger;

        public productService(appDbContext AppDbContext, ILogger<productService> logger)
        {
            context = AppDbContext;
            _logger = logger;
        }

        public productResponse AddProduct(product_requirement productRequest)
        {
            try
            {
                var conn = context.Database.GetDbConnection();
                _logger.LogInformation("Db connection string (redact credentials): {Conn}", conn.ConnectionString);
                _logger.LogInformation("Database can connect: {CanConnect}", context.Database.CanConnect());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read DB connection or check connectivity.");
            }

            var product = new Product
            {
                Id = 0,
                Name = productRequest.Name,
                Description = productRequest.Description,
                Price = productRequest.Price
            };

            var newproduct = context.Products.Add(product);
            context.SaveChanges();

            var response = new productResponse
            {
                Id = newproduct.Entity.Id,
                Name = newproduct.Entity.Name,
                Description = newproduct.Entity.Description,
                Price = newproduct.Entity.Price
            };

            return response;
        }

        public void DeleteProduct(int id)
        {
            var product = context.Products.Find(id);
            if (product != null)
            {
                context.Products.Remove(product);
                context.SaveChanges();
            }
        }

        public IEnumerable<Product> GetAllProducts()
        {
            var products = context.Products.ToList();

            var response = products.Select(p => new productResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price
            });

            return products;
        }

        public productResponse? GetProductById(int id)
        {
            var product = context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null) return null;

            var response = new productResponse
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price
            };

            return response;
        }

        public void UpdateProduct(Product product)
        {
            var existingProduct = context.Products.FirstOrDefault(p => p.Id == product.Id);
            if (existingProduct != null)
            {
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                context.SaveChanges();
            }
        }

        public void UpdateProduct(int id, Product product)
        {
            throw new NotImplementedException();
        }
    }
}