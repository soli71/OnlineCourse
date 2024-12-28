namespace OnlineCourse.Entities;

public class Cart
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public CartStatus Status { get; set; }

    public List<CartItem> CartItems { get; set; }
}
