using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineCourse.Identity.Entities;
using System.ComponentModel.DataAnnotations;

namespace OnlineCourse.Orders;

public class OrderStatusHistory
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime Date { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public string Description { get; set; }
}

public class OrderStatusHistoryConfig : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.ClientNoAction);
    }
}

public class OrderConfig : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasOne(o => o.Address)
            .WithMany()
            .HasForeignKey(c => c.AddressId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ForPay { get; set; }
    public DateTime OrderDate { get; set; }
    public string OrderCode { get; set; }
    public OrderStatus Status { get; set; }
    public string ReceiverName { get; set; }
    public string ReceiverPhoneNumber { get; set; }
    public string Description { get; set; }
    public string TrackingCode { get; set; }
    public UserAddress Address { get; set; }
    public int? AddressId { get; set; }
    public List<OrderDetails> OrderDetails { get; set; }
    public DiscountUsage DiscountUsage { get; set; }
}

public enum OrderStatus
{
    [Display(Name = "در انتظار پرداخت")]
    Pending = 1,

    [Display(Name = "پرداخت شده")]
    Paid = 2,

    [Display(Name = "لغو شده")]
    Canceled = 3,

    [Display(Name = "ارسال شده")]
    Sent = 4,
}