using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Controllers.Panel;
using OnlineCourse.Entities;
using OnlineCourse.Extensions;
using System.Security.Claims;

namespace OnlineCourse.Controllers.Site;

[Route("api/site/[controller]")]
[ApiController]
[Authorize(Roles = "User")]
public class OrderController : BaseController
{
    private readonly ApplicationDbContext _context;

    public OrderController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder()
    {
        var user = HttpContext.User;
        var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier));

        var cart = await _context.Carts
            .Include(x => x.CartItems)
            .ThenInclude(x => x.Course)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Status == CartStatus.Active);

        if (cart is null)
        {
            return BadRequestB("سبد خرید شما خالی می باشد");
        }

        decimal totalPrice = 0;
        List<FinalizeCartCourseDto> finalizeCartCourseDtos = new();
        foreach (var cartItem in cart.CartItems)
        {
            var course = await _context.Courses.FindAsync(cartItem.CourseId);

            var totalCourseOrder = await _context.OrderDetails
                .Where(x => x.CourseId == cartItem.CourseId && (x.Order.Status == OrderStatus.Paid || (x.Order.Status == OrderStatus.Pending && x.Order.OrderDate.AddMinutes(60) > DateTime.UtcNow)))
                .CountAsync();

            if (course.Limit > 0 && totalCourseOrder > course.Limit)
            {
                return BadRequestB("ظرفیت دوره تکمیل می باشد");
            }
            if (course is null)
            {
                return BadRequestB("این دوره غیرفعال می باشد");
            }
            totalPrice += course.Price;
        }
        cart.Status = CartStatus.Close;

        var order = new Order
        {
            UserId = userId,
            TotalPrice = totalPrice,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            OrderDetails = cart.CartItems.Select(x => new OrderDetails
            {
                CourseId = x.CourseId,
                Price = x.Price
            }).ToList()
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return OkB();
    }
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var user = HttpContext.User;
        var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier));
        var order = await _context.Orders
            .Include(x => x.OrderDetails)
            .ThenInclude(x => x.Course)
            .Where(x => x.UserId == userId)
            .Select(x => new GetOrderSiteDto(
                 x.Id,
                 x.Status.GetDisplayValue(),
                 x.TotalPrice,
                 x.OrderDate.ToPersianDateTime()
            )).ToListAsync();
        return OkB(order);
    }
}

public record GetOrderSiteDto(int Id, string Status, decimal TotalPrice, string OrderDate);

public record FinalizeCartDto(decimal TotalPrice, List<FinalizeCartCourseDto> FinalizeCartCourseDtos);
public record FinalizeCartCourseDto(int CourseId, string Name, decimal Price, string message);