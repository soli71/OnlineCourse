namespace OnlineCourse.Entities;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime OrderDate { get; set; }

    public OrderStatus Status { get; set; }
}

public enum OrderStatus
{
    Pending = 1,
    Paid = 2,
    Canceled = 3
}