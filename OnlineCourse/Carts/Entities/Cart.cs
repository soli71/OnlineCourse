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

    public List<CartItem> CartItems { get; set; }
}