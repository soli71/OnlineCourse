namespace OnlineCourse.Carts;

public class Cart
{
    public Cart()
    {
        Id = Guid.NewGuid();
    }

    public Guid Id { get; set; }
    public int? UserId { get; set; }
    public CartStatus Status { get; set; }
    public int? DiscountCodeId { get; set; }
    public DiscountCode DiscountCode { get; set; }
    public decimal DiscountAmount { get; set; }

    public List<CartItem> CartItems { get; set; }
}