using OnlineCourse.Products.Entities;

namespace OnlineCourse.Carts;

public class CartItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; }
    public decimal Price { get; set; }
    public decimal DiscountAmount { get; set; }
    public Cart Cart { get; set; }
    public Guid CartId { get; set; }
    public bool IsDelete { get; set; }
    public string Message { get; set; }
    public int Quantity { get; set; }
}