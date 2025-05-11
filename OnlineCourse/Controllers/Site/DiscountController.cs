using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minio.DataModel;
using OnlineCourse.Carts;
using OnlineCourse.Contexts;
using OnlineCourse.Controllers.Panel;
using OnlineCourse.Identity.Entities;
using System.Security.Claims;

namespace OnlineCourse.Controllers.Site
{
    [Route("api/site/[controller]")]
    [ApiController]
    [Authorize(Roles = "User")]
    public class DiscountController : BaseController
    {
        private readonly ApplicationDbContext _dbContext;

        public DiscountController(ApplicationDbContext applicationDbContext)
        {
            _dbContext = applicationDbContext;
        }

        [HttpPatch("apply")]
        public async Task<IActionResult> Apply(string code)
        {
            // دریافت کد تخفیف از دیتابیس همراه با داده‌های مرتبط
            var discount = await _dbContext.DiscountCodes
                .Include(d => d.Products)
                .Include(d => d.AllowedUsers)
                .Include(d => d.Usages)
                .ThenInclude(u => u.Order)
                .FirstOrDefaultAsync(d => d.Code == code);

            if (discount == null)
                return NotFoundB("کد تخفیف یافت نشد.");

            // بررسی تاریخ اعتبار
            if (DateTime.UtcNow < discount.StartDate ||
                (discount.EndDate.HasValue && DateTime.UtcNow > discount.EndDate.Value))
                return NotFoundB("کد تخفیف منقضی شده است.");

            // بررسی محدودیت تعداد استفاده
            if (discount.UsageLimit.HasValue && discount.Usages.Count() >= discount.UsageLimit.Value)
                return NotFoundB("تعداد استفاده از این کد تخفیف به پایان رسیده است.");

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return NotFoundB("کاربر یافت نشد.");
            }

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            // بررسی کاربر مجاز
            if (discount.AllowedUsers.Any() && !discount.AllowedUsers.Contains(user))
                return BadRequestB("شما مجاز به استفاده از این کد تخفیف نیستید.");

            if (discount.Usages.Any(c => c.DiscountCode.Code == code && c.Order.UserId == userId))
                return BadRequestB("شما قبلا از این کد تخفیف استفاده کرده‌اید.");

            var cart = await _dbContext.Carts
                .Include(c => c.DiscountCode)
                .Include(c => c.CartItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == CartStatus.Active);

            if (cart.DiscountCode is not null)
                return BadRequestB("این سبد خرید قبلا کد تخفیف دارد.");

            if (discount.For == DiscountFor.Product)
            {
                // فیلتر آیتم‌های مجاز در سبد (حذف محصولات با تخفیف مستقل و غیر مجاز)
                var eligibleItems = cart.CartItems.Where(item =>
                        // کالایی که خود تخفیف ندارد یا دوره تخفیفش تمام شده باشد
                        (!item.Product.DiscountPrice.HasValue ||
                         item.Product.DiscountPrice == null ||
                         DateTime.UtcNow < item.Product.DiscountStartDate ||
                         DateTime.UtcNow > item.Product.DiscountEndDate) &&

                        // بررسی محدودیت محصول/دسته‌بندی
                        (discount.Products.Count == 0 || discount.Products.Contains(item.Product))
                    ).ToList();

                if (!eligibleItems.Any())
                    return BadRequestB("محصولات سبد خرید شما مشمول این کد تخفیف نیستند.");

                if (eligibleItems.Count > 1)
                {
                    return BadRequestB("فقط یک محصول مجاز به استفاده از این کد تخفیف است.");
                }

                var discountCandidate = eligibleItems.First();
                // محاسبه مقدار تخفیف
                decimal discountAmount = 0;
                if (discount.Type == DiscountType.Percentage)
                {
                    // تخفیف درصدی
                    discountAmount = discountCandidate.Price * discount.Value / 100m;
                }
                else if (discount.Type == DiscountType.Amount)
                {
                    // تخفیف مبلغ ثابت (حداکثر تا مبلغ کل)
                    discountAmount = Math.Min(discount.Value, discountCandidate.Price);
                }

                if (discountCandidate.Price - discountAmount <= 0)
                    return BadRequestB("کد تخفیف نمیتواند شامل تمام قیمت محصول باشد");
                else
                {
                    eligibleItems.First().DiscountAmount = discountAmount;
                }

                cart.DiscountCode = discount;
                cart.DiscountAmount = discountAmount;
            }
            else
            {
                decimal discountAmount = 0;

                if (discount.Type == DiscountType.Percentage)
                {
                    // تخفیف درصدی
                    discountAmount = cart.CartItems.Sum(c => c.Price) * discount.Value / 100m;
                }
                else if (discount.Type == DiscountType.Amount)
                {
                    // تخفیف مبلغ ثابت (حداکثر تا مبلغ کل)
                    discountAmount = Math.Min(discount.Value, cart.CartItems.Sum(c => c.Price));
                }
                if (discount.MinimumOrderValue > cart.CartItems.Sum(c => c.Price))
                    return BadRequestB("حداقل مبلغ سفارش برای استفاده از این کد رعایت نشده است.");

                cart.DiscountCode = discount;
                cart.DiscountAmount = discountAmount;
            }
            await _dbContext.SaveChangesAsync();
            return OkB();
        }
    }
}