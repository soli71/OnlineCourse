using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using OnlineCourse.Carts;
using OnlineCourse.Contexts;
using OnlineCourse.Controllers.Panel;
using OnlineCourse.Extensions;
using OnlineCourse.Identity.Entities;
using OnlineCourse.Orders;
using OnlineCourse.Products.Entities;
using OnlineCourse.Products.Services;
using OnlineCourse.Services;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Security.Claims;

namespace OnlineCourse.Controllers.Site;

public class CreateOrderRequestModel
{
    public string CartId { get; set; }

    public int? AddressId { get; set; }

    public string Description { get; set; }
    public string ReceiverName { get; set; }
    public string ReceiverPhoneNumber { get; set; }
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
    private readonly IMinioService _minioService;

    public OrderController(
        ApplicationDbContext context,
        ICourseCapacityService courseCapacityService,
        ISmsService smsService,
        UserManager<User> userManager,
        IMinioService minioService)
    {
        _context = context;
        _courseCapacityService = courseCapacityService;
        _smsService = smsService;
        _userManager = userManager;
        _minioService = minioService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderRequestModel createOrderRequestDto)
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

        var addressId = createOrderRequestDto.AddressId == default || createOrderRequestDto.AddressId == null ? default : createOrderRequestDto.AddressId;

        if (cart.CartItems.Any(c => c.Product as PhysicalProduct != null))
        {
            if (addressId == default)
                return BadRequestB("سبد خرید شما شامل محصول فیزیکی می باشد. برای خرید محصول فیزیکی انتخاب ادرس الزامی می باشد");

            var existUserAddress = _context.UserAddresses.Any(c => c.Id == addressId && c.UserId == userId);
            if (!existUserAddress)
                return BadRequestB("آدرس کاربر ناصحیح است");
        }
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

                    var existCourseForUser = _context.Orders
                        .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                        .Any(o => o.UserId == userId && o.OrderDetails.Any(od => od.ProductId == course.Id));
                    if (existCourseForUser)
                        return BadRequestB($"شما قبلا  دوره {course.Name} را خریداری کرده اید");

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
                ReceiverName = createOrderRequestDto.ReceiverName,
                ReceiverPhoneNumber = createOrderRequestDto.ReceiverPhoneNumber,
                Description = createOrderRequestDto.Description,
                AddressId = addressId is null ? null : int.Parse(addressId.ToString()),
                OrderCode = $"OC-{seq}",
                OrderDetails = cart.CartItems.Where(c => !c.IsDelete && c.Quantity >= 0).Select(ci => new OrderDetails
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
                o.TrackingCode ?? "-",
        o.OrderDetails.Select(od => new GetOrderDetailSiteDto(
                    od.Product.Name,
                    od.Quantity,
                    od.UnitPrice)
                ).ToList()
            ))
            .ToListAsync();

        return OkB(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

        var order = _context.Orders.Where(c => c.Id == id && c.UserId == userId)
            .Include(c => c.Address)
            .Include(c => c.OrderDetails).ThenInclude(c => c.Product).FirstOrDefault();

        if (order is null)
            return NotFoundB("سفارش یافت نشد");

        GetOrderResponse getOrderResponse = null;
        GetOrderCourseResponse getOrderCourseResponse = null;
        List<GetOrderPhysicalProductResponse> getOrderPhysicalProductResponse = new();
        foreach (var orderDetail in order.OrderDetails)
        {
            if (orderDetail.Product is Course course)
            {
                var license = _context.Licenses.FirstOrDefault(c => c.OrderDetailId == orderDetail.Id && c.UserId == userId);
                getOrderCourseResponse = new GetOrderCourseResponse
                {
                    CourseName = course.Name,
                    Price = orderDetail.UnitPrice,
                    ImageUrl = await _minioService.GetFileUrlAsync(MinioKey.Course, orderDetail.Product.DefaultImageFileName),
                    LicenseKey = license?.Key is null ? order.Status != OrderStatus.Paid ? "در حال حاضر کد لایسنس برای این دوره صادر نشده است" : "  کد لایسنس صادر شده است لطفا با پشتیبانی در تماس باشید" : license.Key
                };
            }
            if (orderDetail.Product is PhysicalProduct physicalProduct)
            {
                getOrderPhysicalProductResponse.Add(new GetOrderPhysicalProductResponse
                {
                    ProductName = physicalProduct.Name,
                    Quantity = orderDetail.Quantity,
                    ImageUrl = await _minioService.GetFileUrlAsync(MinioKey.PhysicalProduct, orderDetail.Product.DefaultImageFileName),
                    UnitPrice = orderDetail.UnitPrice
                });
            }
        }
        getOrderResponse = new GetOrderResponse
        {
            OrderCode = order.OrderCode,
            OrderDate = order.OrderDate.ToPersianDateTime(),
            TotalPrice = order.TotalPrice,
            Status = order.Status.GetDisplayValue(),
            ReceiverName = order.ReceiverName,
            ReceiverPhoneNumber = order.ReceiverPhoneNumber,
            Description = order.Description,
            Course = getOrderCourseResponse,
            Address = order.Address?.Address,
            PostalCode = order.Address?.PostalCode,
            PhysicalProducts = getOrderPhysicalProductResponse
        };
        return OkB(getOrderResponse);
    }
}

public record GetOrderSiteDto(
      int Id,
      string Status,
      decimal TotalPrice,
      string OrderDate,
      string OrderCode,
      string TrackingCode,
      List<GetOrderDetailSiteDto> Details
  );

public record GetOrderDetailSiteDto(
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

public class GetOrderResponse
{
    public string OrderCode { get; set; }
    public string OrderDate { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; }
    public string ReceiverName { get; set; }
    public string ReceiverPhoneNumber { get; set; }
    public string Description { get; set; }
    public string Address { get; set; }
    public string PostalCode { get; set; }
    public GetOrderCourseResponse Course { get; set; }
    public List<GetOrderPhysicalProductResponse> PhysicalProducts { get; set; }
}

public class GetOrderCourseResponse
{
    public string CourseName { get; set; }
    public string LicenseKey { get; set; }
    public decimal Price { get; set; }
    public string ImageUrl { get; set; }
}

public class GetOrderPhysicalProductResponse
{
    public string ProductName { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string ImageUrl { get; set; }
}