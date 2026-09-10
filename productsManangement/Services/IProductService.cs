using productsManangement.Dtos;
using productsManangement.Models;

namespace productsManangement.Services
{
    public interface IProductService
    {
        IEnumerable<productResponse> GetAllProducts();
        productResponse? GetProductById(int id);

        productResponse AddProduct(product_requirement product);

        void UpdateProduct(int id, Product product);

        void DeleteProduct(int id);
    }
}
