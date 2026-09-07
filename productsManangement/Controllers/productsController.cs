using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using productsManangement.Models;

namespace productsManangement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class productsController : ControllerBase
    {

        static List<Product>  products = new List<Product>
            {
                new Product { Id = 1, Name = "Product 1", Description = "Description for Product 1", Price = 10.99M },
                new Product { Id = 2, Name = "Product 2", Description = "Description for Product 2", Price = 19.99M },
                new Product { Id = 3, Name = "Product 3", Description = "Description for Product 3", Price = 5.99M }
    };
    [HttpGet]

        public IActionResult GetProducts()
        {
            // Sample data for demonstration purposes
            
            return Ok(products);
        }

        [HttpGet]
        [Route("{id}")]
        public IActionResult GetProduct(int id)
        {
            var response = products.FirstOrDefault(p => p.Id == id);
            if (response == null)
            {
                return NotFound();
            }
            return Ok(response);
        }

        [HttpPut]
        [Route("{id}")]
        public IActionResult UpdateProduct(int id, Product product)
        {
            var existingProduct = products.FirstOrDefault(p => p.Id == id);
            if (existingProduct == null)
            {
                return NotFound();
            }
            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            return NoContent();
        }
        [HttpDelete]
        [Route("{id}")]
        public IActionResult DeleteProduct(int id)
        {
            var productToDelete = products.FirstOrDefault(p => p.Id == id);
            if (productToDelete == null)
            {
                return NotFound();
            }
            products.Remove(productToDelete);
            return NoContent();
        }
    }
}
