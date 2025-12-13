using FarmazonDemo.Data;
using FarmazonDemo.Models.Dto;
using FarmazonDemo.Models.Dto.ProductDto;
using FarmazonDemo.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FarmazonDemo.Controllers
{


    //localhost:xxxxx/api/products
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController :ControllerBase
    {
        private readonly ApplicationDbContext dbContext;
        public ProductController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        // Get All Product 

        [HttpGet]
        public IActionResult getAllProducts()
        {
            var allProducts = dbContext.Products;

            return Ok(allProducts);

        }

        // Get Product By ID

           [HttpGet("{ProductId:int}")]
             public IActionResult getProductById(int ProductId)
           {

               var Product = dbContext.Products.Find(ProductId);

               if (Product is null)
               {
                   return NotFound();

               }
               else
               {
                   return Ok(Product);
               }

           }

   

       /* [HttpGet("{ProductId:int}")]
        public IActionResult getProductById(int ProductId)
        {
            return Ok(new { hit = true, ProductId });
        }

        */

        // Add Product 
        [HttpPost]
        public IActionResult AddProduct(AddProductDto AddProductDto)
        {

            var productEntity = new Product()
            {
                ProductName = AddProductDto.ProductName,
                ProductDescription = AddProductDto.ProductDescription,
                ProductBarcode = AddProductDto.ProductBarcode
            };

            dbContext.Products.Add(productEntity);
            dbContext.SaveChanges();
            return Ok(productEntity);

        }

        // Update Product 

        [HttpPut("{ProductId:int}")]

        public IActionResult updateProduct(int ProductId, [FromBody] ProductUpdateDto productUpdateDto)
        {

            var product = dbContext.Products.Find(ProductId);
            if (product is null)
            {
                return NotFound();
            }
            product.ProductName = productUpdateDto.ProductName;
            product.ProductBarcode = productUpdateDto.ProductBarcode;
            product.ProductDescription = productUpdateDto.ProductDescription;
            dbContext.SaveChanges();

            return Ok(product);


        }

        // Delete Product 


        [HttpDelete]
        [Route("{ProductId:int}")]

        public IActionResult DeleteProduct(int ProductId)
        {

            var product = dbContext.Products.Find(ProductId);

            if (product is null)
            {

                return NotFound();
            }

            dbContext.Products.Remove(product);
            dbContext.SaveChanges();
            return Ok();

        }



    }
}
