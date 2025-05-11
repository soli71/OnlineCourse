using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Extensions;
using OnlineCourse.Identity.Entities;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace OnlineCourse.Controllers.Panel;

public class DiscountCodeCreateResponseModel : IValidatableObject
{
    public string Code { get; set; }
    public DiscountType Type { get; set; }
    public DiscountFor For { get; set; }
    public decimal Value { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? UsageLimit { get; set; }
    public decimal? MinimumOrderValue { get; set; }
    public List<int>? ProductIds { get; set; }
    public List<int>? AllowedUserIds { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Value <= 0)
        {
            yield return new ValidationResult("مقدار تخفیف باید بزرگتر از صفر باشد.", new[] { nameof(Value) });
        }

        if (Type == DiscountType.Percentage && (Value < 1 || Value > 100))
        {
            yield return new ValidationResult("برای تخفیف درصدی، مقدار باید بین ۱ تا ۱۰۰ باشد.", new[] { nameof(Value) });
        }

        if (For == DiscountFor.Product && !ProductIds.Any())
            yield return new ValidationResult("برای تخفیف محصول، لیست شناسه‌های محصولات الزامی است.", new[] { nameof(ProductIds) });

        if (StartDate == default)
        {
            yield return new ValidationResult("تاریخ شروع الزامی است.", new[] { nameof(StartDate) });
        }

        if (EndDate.HasValue && EndDate <= StartDate)
        {
            yield return new ValidationResult("تاریخ پایان باید بعد از تاریخ شروع باشد.", new[] { nameof(EndDate) });
        }

        if (UsageLimit.HasValue && UsageLimit.Value <= 0)
        {
            yield return new ValidationResult("حد استفاده باید بزرگتر از صفر باشد.", new[] { nameof(UsageLimit) });
        }

        if (MinimumOrderValue.HasValue && MinimumOrderValue.Value <= 0)
        {
            yield return new ValidationResult("حداقل مبلغ سفارش باید بزرگتر از صفر باشد.", new[] { nameof(MinimumOrderValue) });
        }

        // در صورت ارسال لیست‌ها، باید حاوی حداقل یک مقدار باشند
        if (ProductIds != null && ProductIds.Count == 0)
        {
            yield return new ValidationResult("لیست شناسه‌های محصول، در صورت ارسال، نمی‌تواند خالی باشد.", new[] { nameof(ProductIds) });
        }

        if (AllowedUserIds != null && AllowedUserIds.Count == 0)
        {
            yield return new ValidationResult("لیست شناسه‌های کاربران مجاز، در صورت ارسال، نمی‌تواند خالی باشد.", new[] { nameof(AllowedUserIds) });
        }
    }
}

public record DiscountCodeUpdateResponseModel(
    int Id,
    string Code,
    DiscountType Type,
    decimal Value,
    DateTime StartDate,
    DateTime? EndDate,
    int? UsageLimit,
    decimal? MinimumOrderValue,
    List<int>? ProductIds,
    List<int>? AllowedUserIds);

public record DiscountCodeResponseModel(
    int Id,
    string Code,
    DiscountType Type,
    decimal Value,
    string StartDate,
    string EndDate,
    int? UsageLimit,
    int UsedCount,
    decimal? MinimumOrderValue);

public record GetDiscountCodeResponseModel(
    int Id,
    string Code,
    DiscountType Type,
    decimal Value,
    string StartDate,
    string EndDate,
    int? UsageLimit,
    int UsedCount,
    decimal? MinimumOrderValue,
    List<int>? productIds,
    List<int>? allowUsers);

[Route("api/panel/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Panel")]
public class DiscountController : BaseController
{
    private readonly ApplicationDbContext _ctx;

    public DiscountController(ApplicationDbContext ctx)
    {
        _ctx = ctx;
    }

    // POST: api/admin/discount-codes
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DiscountCodeCreateResponseModel dto)
    {
        if (await _ctx.DiscountCodes.AnyAsync(d => d.Code == dto.Code))
            return BadRequestB("کد تخفیف تکراری است.");

        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        var entity = new DiscountCode
        {
            Code = dto.Code,
            Type = dto.Type,
            Value = dto.Value,
            StartDate = dto.StartDate.ToUniversalTime(),
            EndDate = dto.EndDate.HasValue ? dto.EndDate.Value.ToUniversalTime() : null,
            UsageLimit = dto.UsageLimit,
            MinimumOrderValue = dto.MinimumOrderValue,
            CreatedAt = DateTime.UtcNow,
            For = dto.For,
            CreatedBy = userId
            // روابط Products, Categories, Users در متد CreateAsync ست می‌شود
        };

        if (_ctx.DiscountCodes.Any(d => d.Code == dto.Code))
            return BadRequestB("کد تخفیف تکراری است.");

        // روابط اختیاری
        if (dto.ProductIds?.Any() == true)
            entity.Products = await _ctx.Products.Where(p => dto.ProductIds.Contains(p.Id)).ToListAsync();

        if (dto.AllowedUserIds?.Any() == true)
            entity.AllowedUsers = await _ctx.Users.Where(u => dto.AllowedUserIds.Contains(u.Id)).ToListAsync();

        _ctx.DiscountCodes.Add(entity);
        await _ctx.SaveChangesAsync();

        return OkB();
    }

    // GET: api/admin/discount-codes
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PagedRequest filter)
    {
        var query = _ctx.DiscountCodes
           .Include(d => d.Usages)
           .AsQueryable();

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(d => d.Id)
            .Skip(total * (filter.PageNumber - 1)) // محاسبه مقدار شروع
            .Take(filter.PageSize)
            .Select(d => new DiscountCodeResponseModel(          // Projection مستقیم
                d.Id, d.Code, d.Type, d.Value,
                d.StartDate.ToPersianDateTime(), d.EndDate.HasValue ? d.EndDate.Value.ToPersianDateTime() : "-", d.UsageLimit,
                d.Usages.Count,
                d.MinimumOrderValue)).ToListAsync();
        return OkB(new PagedResponse<List<DiscountCodeResponseModel>>()
        {
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            Result = items,
            TotalCount = total
        });
    }

    // GET: api/admin/discount-codes/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get([FromRoute] int id)
    {
        var entity = await _ctx.DiscountCodes
            .Include(d => d.Products)
            .Include(d => d.Usages)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (entity == null) return NotFoundB("کد تخفیف یافت نشد.");
        var dto = new GetDiscountCodeResponseModel(
            entity.Id, entity.Code, entity.Type, entity.Value,
            entity.StartDate.ToPersianDateTime(), entity.EndDate.HasValue ? entity.EndDate.Value.ToPersianDateTime() : "-", entity.UsageLimit,
            entity.Usages.Count,
            entity.MinimumOrderValue,
            entity.Products.Select(p => p.Id).ToList(),
            entity.AllowedUsers?.Select(u => u.Id).ToList() ?? new());
        return OkB(dto);
    }

    // PUT: api/admin/discount-codes/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] DiscountCodeUpdateResponseModel dto)
    {
        var discount = await _ctx.DiscountCodes
            .Include(d => d.Products)
            .Include(d => d.Usages)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (discount == null) return NotFoundB("کد تخفیف یافت نشد.");

        if (await _ctx.DiscountCodes.AnyAsync(d => d.Code == dto.Code && d.Id != id))
            return BadRequestB("کد تخفیف تکراری است.");

        discount.Code = dto.Code;
        discount.Type = dto.Type;
        discount.Value = dto.Value;
        discount.StartDate = dto.StartDate.ToUniversalTime();
        discount.EndDate = dto.EndDate.HasValue ? dto.EndDate.Value.ToUniversalTime() : null;
        discount.UsageLimit = dto.UsageLimit;
        discount.MinimumOrderValue = dto.MinimumOrderValue;
        // روابط اختیاری
        if (dto.ProductIds?.Any() == true)
            discount.Products = await _ctx.Products.Where(p => dto.ProductIds.Contains(p.Id)).ToListAsync();
        if (dto.AllowedUserIds?.Any() == true)
            discount.AllowedUsers = await _ctx.Users.Where(u => dto.AllowedUserIds.Contains(u.Id)).ToListAsync();
        await _ctx.SaveChangesAsync();
        return OkB();
    }

    // DELETE: api/admin/discount-codes/{id}
    //[HttpDelete("{id:int}")]
    //public async Task<IActionResult> Delete(int id)
    //{
    //    var discount = _ctx.DiscountCodes.FirstOrDefault(c => c.Id == id);
    //    if (discount == null) return NotFoundB("کد تخفیف یافت نشد.");
    //    _ctx.DiscountCodes.Remove(discount);
    //    await _ctx.SaveChangesAsync();
    //    return OkB();
    //}
}