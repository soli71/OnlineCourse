using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;

namespace OnlineCourse.Controllers.Site
{
    [Route("api/panel/[controller]")]
    [ApiController]
    public class UserCourseSiteController
    {
        private readonly ApplicationDbContext _applicationDbContext;

        public UserCourseSiteController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        [HttpGet]
        public async Task<List<GetAllUserSiteCoursesDto>> GetAllUserCourses(int userId)
        {
            var userCourses = await _applicationDbContext.UserCourses
                .Where(x => x.UserId == userId)
                .Select(x => new GetAllUserSiteCoursesDto(x.Course.Id, x.Course.Name, x.Course.Price, x.License, x.Key))
                .ToListAsync();
            return userCourses;
        }
    }

    public record GetAllUserSiteCoursesDto(int Id, string Name, decimal Price, string License, string Key);
}