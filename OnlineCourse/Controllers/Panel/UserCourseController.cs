using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Entities;
using System.Net.Quic;

namespace OnlineCourse.Controllers.Panel;

[Route("api/panel/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Panel")]
public class UserCourseController : BaseController
{
    private readonly ApplicationDbContext _applicationDbContext;

    public UserCourseController(ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext;
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetAllUserCourses(
    [FromRoute] int userId,
    [FromQuery] PagedRequest pagedRequest)
    {
        // ۱) شامل کردن Order و Product و فقط جزئیات دوره‌ها (Discriminator = "Course")
        var query = _applicationDbContext.OrderDetails
            .Include(od => od.Order)
            .Include(od => od.Product)
            .Where(od => od.Order.UserId == userId
                      && od.Order.Status == OrderStatus.Paid
                      && od.Product is Course)
            .AsQueryable();

        // ۲) فیلتر جستجو روی نام دوره (cast به Course)
        if (!string.IsNullOrEmpty(pagedRequest.Search))
        {
            query = query.Where(od =>
                ((Course)od.Product).Name.Contains(pagedRequest.Search));
        }

        // ۳) شمارش کل
        var totalCount = await query.CountAsync();

        // ۴) صفحه‌بندی و Select نهایی
        var items = await query
            .Skip((pagedRequest.PageNumber - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .ToListAsync();

        var resultItems = items.Select(od =>
        {
            var course = (Course)od.Product;
            return new GetAllUserCoursesDto(
                Id: course.Id,
                Name: course.Name,
                Price: course.Price,
                License: "od.License",
                Key: "od.Key"
            );
        }).ToList();

        // ۵) برگرداندن پاسخ
        return OkB(new PagedResponse<List<GetAllUserCoursesDto>>
        {
            PageNumber = pagedRequest.PageNumber,
            PageSize = pagedRequest.PageSize,
            TotalCount = totalCount,
            Result = resultItems
        });
    }
}

public record GetAllUserCoursesDto(int Id, string Name, decimal Price, string License, string Key);