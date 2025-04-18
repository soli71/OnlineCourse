namespace OnlineCourse.Entities;

public class OrderDetails
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Description { get; set; }
}

public class License
{
    public int Id { get; set; }

    public string Key { get; set; }

    public int UserId { get; set; }

    public User User { get; set; }

    public int OrderDetailId { get; set; }

    public OrderDetails OrderDetail { get; set; }

    public DateTime IssuedDate { get; set; }

    public DateTime? ExpirationDate { get; set; }

    public LicenseStatus Status { get; set; }
}

public enum LicenseStatus
{
    Active,
    Revoked,
    Expired
}