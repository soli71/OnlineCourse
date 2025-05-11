// مدل‌های داده
using OnlineCourse.Identity.Entities;
using OnlineCourse.Products.Entities;

public enum DiscountType
{ Percentage, Amount, FreeShipping }

public class DiscountCode
{
    public int Id { get; set; }
    public string Code { get; set; }
    public DiscountType Type { get; set; }
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
}

public class DiscountUsage
{
    public int Id { get; set; }
    public int DiscountCodeId { get; set; }
    public DiscountCode DiscountCode { get; set; }
    public string UserId { get; set; }
    public User User { get; set; }
    public int OrderId { get; set; }
    public DateTime UsedAt { get; set; }
}