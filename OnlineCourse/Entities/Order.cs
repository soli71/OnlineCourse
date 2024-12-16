using System.ComponentModel.DataAnnotations;

namespace OnlineCourse.Entities;

public class OrderStatusHistory
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime Date { get; set; }
    public int UserId { get; set; }
    public string Description { get; set; }
}
public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime OrderDate { get; set; }
    public string OrderCode { get; set; }
    public OrderStatus Status { get; set; }
    public List<OrderDetails> OrderDetails { get; set; }
}

public enum OrderStatus
{
    [Display(Name = "در انتظار پرداخت")]
    Pending = 1,
    [Display(Name = "پرداخت شده")]
    Paid = 2,
    [Display(Name = "لغو شده")]
    Canceled = 3
}