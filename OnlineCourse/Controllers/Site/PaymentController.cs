using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Entities;
using OnlineCourse.Services;

namespace OnlineCourse.Controllers.Site;

public class PaymentController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICourseCapacityService _courseCapacityService;

    public PaymentController(ApplicationDbContext context, ICourseCapacityService courseCapacityService)
    {
        _context = context;
        _courseCapacityService = courseCapacityService;
    }

    [HttpGet]
    public async Task<IActionResult> CheckPayment([FromBody] CreatePaymentDto createPaymentDto)
    {
        decimal totalPrice = 0m;
        var finalizeItems = new List<FinalizeCartItemDto>();

        foreach (var productId in createPaymentDto.ProductIds)
        {
            var product = await _context.Products
                .FindAsync(productId);

            if (product == null)
            {
                finalizeItems.Add(new FinalizeCartItemDto(
                    productId, null, 0m, "محصول یافت نشد"));
                continue;
            }

            switch (product)
            {
                case Course course:
                    // بررسی ظرفیت دوره

                    if (!await _courseCapacityService.ExistCourseCapacityAsync(course.Id))
                    {
                        finalizeItems.Add(new FinalizeCartItemDto(
                            course.Id, course.Name, course.Price,
                            "ظرفیت دوره تکمیل می‌باشد"));
                    }
                    else
                    {
                        finalizeItems.Add(new FinalizeCartItemDto(
                            course.Id, course.Name, course.Price, null));
                        totalPrice += course.Price;
                    }
                    break;

                case PhysicalProduct phys:
                    // بررسی موجودی محصول فیزیکی
                    if (phys.StockQuantity <= 0)
                    {
                        finalizeItems.Add(new FinalizeCartItemDto(
                            phys.Id, phys.Name, phys.Price,
                            "موجودی محصول کافی نیست"));
                    }
                    else
                    {
                        finalizeItems.Add(new FinalizeCartItemDto(
                            phys.Id, phys.Name, phys.Price, null));
                        totalPrice += phys.Price;
                    }
                    break;

                default:
                    finalizeItems.Add(new FinalizeCartItemDto(
                        productId, product.Name, product.Price,
                        "نوع محصول نامعتبر است"));
                    break;
            }
        }

        return Ok(new FinalizeCartDto(totalPrice, finalizeItems));
    }
}

public record CreatePaymentDto(int[] ProductIds);

public record FinalizeCartItemDto(
    int ProductId,
    string Name,
    decimal Price,
    string Message
);

public record FinalizeCartDto(
    decimal TotalPrice,
    List<FinalizeCartItemDto> Items
);