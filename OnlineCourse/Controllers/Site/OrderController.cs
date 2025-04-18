using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Controllers.Panel;
using OnlineCourse.Entities;
using OnlineCourse.Extensions;
using OnlineCourse.Services;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Security.Claims;

namespace OnlineCourse.Controllers.Site;

public class CreateOrderRequestDto
{
    public string CartId { get; set; }
}

[Route("api/site/[controller]")]
[ApiController]
[Authorize(Roles = "User")]
public class OrderController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly ICourseCapacityService _courseCapacityService;
    private readonly UserManager<User> _userManager;
    private readonly ISmsService _smsService;
    private readonly object _lock = new();

    public OrderController(ApplicationDbContext context, ICourseCapacityService courseCapacityService, ISmsService smsService, UserManager<User> userManager)
    {
        _context = context;
        _courseCapacityService = courseCapacityService;
        _smsService = smsService;
        _userManager = userManager;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderRequestDto createOrderRequestDto)
    {
        Expression<Func<Cart, bool>> predict = c => c.UserId == 33333333333;
        int userId = 0;
        if (string.IsNullOrEmpty(createOrderRequestDto.CartId))
        {
            var user = HttpContext.User;
            userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier));

            predict = c => (c.UserId == userId && c.Status == CartStatus.Active);
        }
        else
        {
            Guid.TryParse(createOrderRequestDto.CartId, out var cartId);
            predict = c => c.Id == cartId && c.Status == CartStatus.Active;
        }

        var cart = await _context.Carts
            .Include(x => x.CartItems)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(predict);

        if (cart == null || !cart.CartItems.Any())
            return BadRequestB("سبد خرید شما خالی می باشد");

        decimal totalPrice = 0;
        foreach (var ci in cart.CartItems)
        {
            var product = ci.Product;

            switch (product)
            {
                case Course course:
                    if (!course.IsPublish)
                        return BadRequestB($"دوره '{course.Name}' در دسترس نیست");
                    if (!await _courseCapacityService.ExistCourseCapacityAsync(course.Id))
                        return BadRequestB($"ظرفیت دوره '{course.Name}' تکمیل می باشد");

                    totalPrice += course.Price * ci.Quantity;
                    break;

                case PhysicalProduct phys:
                    if (phys.StockQuantity < ci.Quantity)
                        return BadRequestB($"موجودی محصول '{phys.Name}' کافی نیست");

                    totalPrice += phys.Price * ci.Quantity;
                    break;

                default:
                    return BadRequestB("نوع محصول نامعتبر است");
            }
        }

        cart.Status = CartStatus.Close;

        Order order = new();
        //using (_lock.EnterScope())
        //{
        cart.Status = CartStatus.Close;

        lock (_lock)
        {
            var seq = GetNextOrderCode();
            order = new Order
            {
                UserId = userId,
                TotalPrice = totalPrice,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                OrderCode = $"OC-{seq}",
                OrderDetails = cart.CartItems.Select(ci => new OrderDetails
                {
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Price
                }).ToList()
            };
            _context.Orders.Add(order);
            _context.SaveChanges();
        }
        //}

        //read admin phone number from environment variable
        var adminPhoneNumber = Environment.GetEnvironmentVariable("AdminPhoneNumber");
        await _smsService.SendCreateOrderMessageForAdmin(adminPhoneNumber, order.OrderCode, order.OrderDetails.FirstOrDefault().Product.Name, order.OrderDate.ToPersianDateTime());

        var userForMessage = await _userManager.FindByIdAsync(userId.ToString());

        Debug.Assert(userForMessage is null);

        //send sms to user
        await _smsService.SendCreateOrderMessageForUser(userForMessage.PhoneNumber, order.OrderCode);
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
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .OrderByDescending(o => o.OrderDate)

            .Select(o => new GetOrderSiteDto(
                o.Id,
                o.Status.GetDisplayValue(),
                o.TotalPrice,
                o.OrderDate.ToPersianDateTime(),
                o.OrderCode,
                o.OrderDetails.Select(od => new GetOrderDetailSiteDto(
                    od.Product.Name,
                    od.Quantity,
                    od.UnitPrice)
                ).ToList()
            ))
            .ToListAsync();

        return OkB(orders);
    }
}

public record GetOrderSiteDto(
      int Id,
      string Status,
      decimal TotalPrice,
      string OrderDate,
      string OrderCode,
      List<GetOrderDetailSiteDto> Details
  );

public record GetOrderDetailSiteDto(
    string ProductName,
    int Quantity,
    decimal UnitPrice
);