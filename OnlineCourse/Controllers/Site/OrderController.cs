using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Controllers.Panel;
using OnlineCourse.Entities;
using OnlineCourse.Extensions;
using OnlineCourse.Services;
using System.Security.Claims;

namespace OnlineCourse.Controllers.Site;

[Route("api/site/[controller]")]
[ApiController]
[Authorize(Roles = "User")]
public class OrderController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly ICourseCapacityService _courseCapacityService;
    private readonly ISmsService _smsService;
    private readonly Lock _lock = new();

    public OrderController(ApplicationDbContext context, ICourseCapacityService courseCapacityService, ISmsService smsService)
    {
        _context = context;
        _courseCapacityService = courseCapacityService;
        _smsService = smsService;
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
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == cartItem.CourseId && c.IsPublish);

            if (course is null)
            {
                return BadRequestB("این دوره غیرفعال می باشد");
            }

            var courseCapacity = await _courseCapacityService.ExistCourseCapacityAsync(cartItem.CourseId);
            if (!courseCapacity)
            {
                return BadRequestB("ظرفیت دوره تکمیل می باشد");
            }

            totalPrice += course.Price;
        }
        cart.Status = CartStatus.Close;

        Order order = new();
        using (_lock.EnterScope())
        {
            int orderCodeSequence = GetNextOrderCode();

            order = new Order
            {
                UserId = userId,
                TotalPrice = totalPrice,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                OrderCode = $"OC-{orderCodeSequence}",
                OrderDetails = cart.CartItems.Select(x => new OrderDetails
                {
                    CourseId = x.CourseId,
                    Price = x.Price
                }).ToList()
            };
            _context.Orders.Add(order);
            _context.SaveChanges();
        }

        //read admin phone number from environment variable
        var adminPhoneNumber = Environment.GetEnvironmentVariable("AdminPhoneNumber");
        await _smsService.SendCreateOrderMessageForAdmin(adminPhoneNumber, order.OrderCode, order.OrderDetails.FirstOrDefault().Course.Name, order.OrderDate.ToPersianDateTime());

        //send sms to user
        await _smsService.SendCreateOrderMessageForUser(user.FindFirstValue(ClaimTypes.MobilePhone), order.OrderCode);
        return OkB();
    }

    private int GetNextOrderCode()
    {
        var lastOrderCode = _context.Orders.OrderByDescending(x => x.Id).FirstOrDefault()?.OrderCode;
        //get order code sequence
        var orderCodeSequence = 2000;
        if (!string.IsNullOrEmpty(lastOrderCode))
        {
            orderCodeSequence = int.Parse(lastOrderCode.Split('-')[1]) + 1;
        }
        return orderCodeSequence;
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
                 x.OrderDate.ToPersianDateTime(),
                    x.OrderCode
            )).ToListAsync();
        return OkB(order);
    }
}

public record GetOrderSiteDto(int Id, string Status, decimal TotalPrice, string OrderDate, string OrderCode);

public record FinalizeCartDto(decimal TotalPrice, List<FinalizeCartCourseDto> FinalizeCartCourseDtos);
public record FinalizeCartCourseDto(int CourseId, string Name, decimal Price, string message);