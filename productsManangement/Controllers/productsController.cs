using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using productsManangement.Dtos;
using productsManangement.Models;
using productsManangement.Services;

namespace productsManangement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class productsController : ControllerBase
    {

    //    static List<Product>  products = new List<Product>



        private readonly IProductService  service;

        public productsController(IProductService productService)
        {
            service = productService;
        }



        [HttpGet]

        public IActionResult GetProducts()
        {
            // Sample data for demonstration purposes
            
            return Ok(service.GetAllProducts());
        }

        [HttpGet]
        [Route("{id}")]
        public IActionResult GetProduct(int id)
        {
            var response = service.GetProductById(id);
            if (response == null)
            {
                return NotFound();
            }
            return Ok(response);
        }

        [HttpPost]
        public IActionResult AddProduct(product_requirement product)
        {
            var createdProduct = service.AddProduct(product);
            return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.Id }, createdProduct);
        }

        [HttpPut]
        [Route("{id}")]
        public IActionResult UpdateProduct(int id, Product product)
        {


             
            try
            {
                service.UpdateProduct(id, product);

                return NoContent();

            }

            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
        [HttpDelete]
        [Route("{id}")]
        public IActionResult DeleteProduct(int id)
        {

            try
            {
                service.DeleteProduct(id);

                return NoContent();

            }

            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
