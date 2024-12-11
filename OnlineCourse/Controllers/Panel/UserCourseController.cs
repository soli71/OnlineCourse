using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
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
    public async Task<IActionResult> GetAllUserCourses([FromRoute]int userId,[FromQuery]PagedRequest pagedRequest)
    {
        var query= _applicationDbContext.OrderDetails.Include(x => x.Order).Where(x => x.Order.UserId == userId && x.Order.Status == Entities.OrderStatus.Paid).AsQueryable();

        if (!string.IsNullOrEmpty(pagedRequest.Search))
        {
            query = query.Where(x => x.Course.Name.Contains(pagedRequest.Search));
        }
        var totalCount = await query.CountAsync();
        query = query.Skip((pagedRequest.PageNumber - 1) * pagedRequest.PageSize).Take(pagedRequest.PageSize);
        var userCourses = query
            
            .Select(x => new GetAllUserCoursesDto(
                x.Course.Id,
                x.Course.Name,
                x.Course.Price,
                x.License,
                x.Key
            ))
            .ToListAsync();
        return OkB(new PagedResponse<List<GetAllUserCoursesDto>>
        {
            PageNumber = pagedRequest.PageNumber,
            PageSize = pagedRequest.PageSize,
            Result = await userCourses,
            TotalCount = totalCount
        });
    }
}

public record GetAllUserCoursesDto(int Id, string Name, decimal Price, string License, string Key);