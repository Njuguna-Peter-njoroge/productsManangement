using productsManangement.Data;
using productsManangement.Dtos;
using productsManangement.Models;

namespace productsManangement.Services
{


    public class productService : IProductService
    {
        private readonly appDbContext context;
        public productService(appDbContext AppDbContext)
        {
            context = AppDbContext;
        }


        public productResponse AddProduct(product_requirement productRequest)
        {

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

var     product = context.Products.Find(id);
            if(product !=null)
            {
                context.Products.Remove(product);
                context.SaveChanges();
            }

        }

        public IEnumerable<productResponse> GetAllProducts()
        {
            var products = context.Products.ToList();


            var response = products.Select(p => new productResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price


            });



            return response;   

        }

        public productResponse? GetProductById(int id)
        {

            var product = context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null) return null;

            var response = product == null ? null   : new productResponse
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price
            };

            return response;
        }

        public Product? UpdateProduct(int id, Product product)
        {
            var existingProduct = context.Products.FirstOrDefault(p => p.Id == product.Id);
            if (existingProduct != null)
            {
                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                context.SaveChanges();
            }

            return existingProduct;


        }


                 
      
    }
}
