using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;

namespace OnlineCourse.Controllers.Panel;

[Route("api/panel/[controller]")]
[ApiController]
public class UserCoursePanelController : ControllerBase
{
    private readonly ApplicationDbContext _applicationDbContext;

    public UserCoursePanelController(ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllUserCourses(int userId)
    {
        var userCourses = await _applicationDbContext.UserCourses
            .Where(x => x.UserId == userId)
            .Select(x => new GetAllUserCoursesDto(x.Course.Id, x.Course.Name, x.Course.Price, x.License, x.Key))
            .ToListAsync();
        return Ok(userCourses);
    }
}

public record GetAllUserCoursesDto(int Id, string Name, decimal Price, string License, string Key);