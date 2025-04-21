using OnlineCourse.Contexts;
using OnlineCourse.Products.Entities;

namespace OnlineCourse.Products.Services
{
    public class PhysicalProductService
    {
        private readonly ApplicationDbContext _applicationDbContext;

        public PhysicalProductService(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        public int GetStockQuantity(int productId)
        {
            var product = _applicationDbContext.Products
                .OfType<PhysicalProduct>()
                .FirstOrDefault(p => p.Id == productId);

            if (product == null)
                return 0;

            var orderedProduct = _applicationDbContext.OrderDetails
                .Where(c => c.ProductId == productId && (c.Order.Status == Orders.OrderStatus.Pending && c.Order.OrderDate.AddMinutes(30) > DateTime.UtcNow));

            var orderedQuantity = orderedProduct.Sum(c => c.Quantity);

            var stockQuantity = product.StockQuantity - orderedQuantity;
            return stockQuantity;
        }
    }
}