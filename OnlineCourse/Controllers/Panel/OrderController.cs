using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Entities;
using OnlineCourse.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace OnlineCourse.Controllers.Panel;

public class PagedRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string Search { get; set; }
}
public class PagedResponse<T> where T : class
{
    public T Result { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public required int TotalCount{get;set;}
}

public class GetAllOrdersDto
{
    public int Id { get; init; }
    public string UserPhoneNumber { get; init; }
    public string Status { get; init; }
    public string PaymentMethod { get; init; }
    public decimal TotalPrice { get; init; }
    public string OrderDate { get; init; }
}

public class GetOrderDto
{
    public int Id { get; init; }
    public string UserPhoneNumber { get; init; }
    public string Status { get; init; }
    public string PaymentMethod { get; init; }
    public decimal TotalPrice { get; init; }
    public string OrderDate { get; init; }
    public List<GetOrderDetailsDto> OrderDetails { get; init; }
}
public class GetOrderDetailsDto
{
    public int Id { get; init; }
    public string CourseName { get; init; }
    public decimal Price { get; init; }
    public string License { get; init; }
    public string Key { get; init; }
}
public class ChangeStatusDto
{
    [Required]
    public OrderStatus Status { get; init; }

    [Required]
    public string Description { get; init; }
}

[Route("api/panel/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Panel")]

public class OrderController : BaseController
{
    private readonly ApplicationDbContext _context;
    public OrderController(ApplicationDbContext context)
    {
        _context = context;
    }
    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery]PagedRequest pagedRequest)
    {
        var query = _context.Orders.AsQueryable();

        if (string.IsNullOrEmpty(pagedRequest.Search))
            query = query.Where(c => c.User.PhoneNumber.Contains(pagedRequest.Search));

        var totalCount =  query.Count();
        query = query.OrderByDescending(c=>c.OrderDate).Skip((pagedRequest.PageNumber - 1) * pagedRequest.PageSize).Take(pagedRequest.PageSize);
        var result = await query.Select(c => new GetAllOrdersDto
        {
            Status = c.Status.GetDisplayValue(),
            UserPhoneNumber = c.User.PhoneNumber,
            OrderDate = c.OrderDate.ToPersianDateTime(),
            Id = c.Id,
            TotalPrice = c.TotalPrice,
        }).ToListAsync();

        var response = new PagedResponse<List<GetAllOrdersDto>>
        {
            PageNumber = pagedRequest.PageNumber,
            PageSize = pagedRequest.PageSize,
            Result = result,
            TotalCount= totalCount
        };
        return OkB(response);
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var order = await _context.Orders.Include(c=>c.OrderDetails).FirstOrDefaultAsync(c=>c.Id==id);
        if (order == null)
        {
            return NotFoundB("سفارش مورد نظر یافت نشد");
        }

        return OkB(new GetOrderDto
        {
            Id = order.Id,
            UserPhoneNumber = order.User.PhoneNumber,
            Status = order.Status.GetDisplayValue(),
            //PaymentMethod = order.PaymentMethod.ToString(),
            TotalPrice = order.TotalPrice,
            OrderDate = order.OrderDate.ToPersianDateTime(),
            OrderDetails = order.OrderDetails.Select(c => new GetOrderDetailsDto
            {
                CourseName = c.Course.Name,
                Id = c.Id,
                Key = c.Key,
                License = c.License,
                Price = c.Price
            }).ToList()
        });
    }

    [HttpPatch("{id}/change-status")]
    public async Task<IActionResult> ChangeStatus(int id, ChangeStatusDto changeStatusDto)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
        {
            return NotFoundB("سفارش مورد نظر یافت نشد");
        }
        if(changeStatusDto.Status==OrderStatus.Paid)
        {
        
            var orderDetails = await _context.OrderDetails.Where(c => c.OrderId == id).ToListAsync();
            foreach (var orderDetail in orderDetails)
            {
                var course = await _context.Courses.FindAsync(orderDetail.CourseId);
                if (course.Limit > 0)
                {
                    var totalCourseOrder = await _context.OrderDetails
                        .Where(x => x.CourseId == orderDetail.CourseId && (x.Order.Status == OrderStatus.Paid || (x.Order.Status == OrderStatus.Pending && x.Order.OrderDate.AddMinutes(60) > DateTime.UtcNow)))
                        .CountAsync();
                    if (totalCourseOrder > course.Limit)
                    {
                        return BadRequestB("ظرفیت دوره تکمیل می باشد");
                    }
                }
                //TODO:SpotPlayer
                //orderDetail.Key = Guid.NewGuid().ToString();
                //orderDetail.License = Guid.NewGuid().ToString();

            }
        }
        order.Status = changeStatusDto.Status;
        var orderStatusHistory = new OrderStatusHistory
        {
            OrderId = order.Id,
            Status = changeStatusDto.Status,
            Description = changeStatusDto.Description,
            UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
            Date = DateTime.UtcNow
        };
        _context.OrderStatusHistories.Add(orderStatusHistory);
        await _context.SaveChangesAsync();
        return OkB();
    }
    [HttpGet("status")]
    public IActionResult GetOrderStatus()
    {
        return OkB(Enum.GetNames<OrderStatus>().Select(c => new { Value = c, DisplayName = EnumHelper<OrderStatus>.GetDisplayValue(c) }));
    }
}

