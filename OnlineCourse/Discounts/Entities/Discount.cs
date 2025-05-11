// مدل‌های داده
using OnlineCourse.Identity.Entities;
using OnlineCourse.Orders;
using OnlineCourse.Products.Entities;

public enum DiscountType
{ Percentage = 1, Amount = 2, FreeShipping = 3 }

public enum DiscountFor
{
    Product = 1,
    Cart = 2
}

public class DiscountCode : ICreatedAudit
{
    public int Id { get; set; }
    public string Code { get; set; }
    public DiscountType Type { get; set; }
    public DiscountFor For { get; set; } // نوع تخفیف (محصول یا سبد خرید)
    public decimal Value { get; set; }             // درصد یا مبلغ تخفیف
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? UsageLimit { get; set; }
    public decimal? MinimumOrderValue { get; set; }

    // فهرست محصولات/دسته‌بندی‌های مشمول تخفیف
    public ICollection<Product> Products { get; set; }

    public ICollection<User> AllowedUsers { get; set; }

    // سابقه استفاده‌ها
    public ICollection<DiscountUsage> Usages { get; set; }

    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
}

public class DiscountUsage
{
    public int Id { get; set; }
    public int DiscountCodeId { get; set; }
    public DiscountCode DiscountCode { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; }
    public DateTime UsedAt { get; set; }
}