using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Entities;

namespace OnlineCourse.Controllers.Site;

public class PaymentController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PaymentController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> CheckPayment([FromBody] CreatePaymentDto createPaymentDto)
    {
        decimal totalPrice = 0;
        List<FinalizeCartCourseDto> finalizeCartCourseDtos = new();
        foreach (var courseId in createPaymentDto.CourseIds)
        {
            var course = await _context.Courses.FindAsync(courseId);

            var totalCourseOrder = await _context.OrderDetails
                .Where(x => x.CourseId == courseId && (x.Order.Status == OrderStatus.Paid || (x.Order.Status == OrderStatus.Pending && x.Order.OrderDate.AddMinutes(30) < DateTime.UtcNow)))
                .CountAsync();

            if (totalCourseOrder > course.Limit)
            {
                finalizeCartCourseDtos.Add(new FinalizeCartCourseDto(course.Id, course.Name, course.Price, "ظرفیت دوره تکمیل می باشد"));
                continue;
            }
            if (course is null)
            {
                finalizeCartCourseDtos.Add(new FinalizeCartCourseDto(course.Id, course.Name, course.Price, "این دوره غیرفعال می باشد"));
                continue;
            }

            finalizeCartCourseDtos.Add(new FinalizeCartCourseDto(course.Id, course.Name, course.Price, ""));
            totalPrice += course.Price;
        }

        return Ok(new FinalizeCartDto(totalPrice, finalizeCartCourseDtos));
    }

    public record CreatePaymentDto(int[] CourseIds);
    public record FinalizeCartDto(decimal TotalPrice, List<FinalizeCartCourseDto> FinalizeCartCourseDtos);
    public record FinalizeCartCourseDto(int CourseId, string Name, decimal Price, string message);
}

[Route("api/site/[controller]")]
[ApiController]
public class OrderController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrderController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto createOrderDto)
    {
        var course = await _context.Courses.FindAsync(createOrderDto.CourseId);
        if (course == null)
        {
            return StatusCode(404, "دوره مورد نظر یافت نشد");
        }

        var order = new Order
        {
            UserId = createOrderDto.UserId,
            CourseId = createOrderDto.CourseId,
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return Ok();
    }
}

public record CreateOrderDto(int UserId, int CourseId);